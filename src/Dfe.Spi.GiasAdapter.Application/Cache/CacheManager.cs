using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models;

namespace Dfe.Spi.GiasAdapter.Application.Cache
{
    public interface ICacheManager
    {
        Task DownloadEstablishmentsToCacheAsync(CancellationToken cancellationToken);
        Task ProcessBatchOfEstablishments(long[] urns, CancellationToken cancellationToken);
    }

    public class CacheManager : ICacheManager
    {
        private readonly IGiasApiClient _giasApiClient;
        private readonly IEstablishmentRepository _establishmentRepository;
        private readonly IMapper _mapper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IEstablishmentProcessingQueue _establishmentProcessingQueue;
        private readonly ILoggerWrapper _logger;

        public CacheManager(
            IGiasApiClient giasApiClient, 
            IEstablishmentRepository establishmentRepository, 
            IMapper mapper,
            IEventPublisher eventPublisher,
            IEstablishmentProcessingQueue establishmentProcessingQueue,
            ILoggerWrapper logger)
        {
            _giasApiClient = giasApiClient;
            _establishmentRepository = establishmentRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
            _establishmentProcessingQueue = establishmentProcessingQueue;
            _logger = logger;
        }
        
        public async Task DownloadEstablishmentsToCacheAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Acquiring establishments file from GIAS...");

            // Download
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            _logger.Info($"Downloaded {establishments.Length} establishments from GIAS");
            
            // Store
            await _establishmentRepository.StoreInStagingAsync(establishments, cancellationToken);
            _logger.Info($"Stored {establishments.Length} establishments in staging");
            
            // Queue diff check
            var position = 0;
            const int batchSize = 1000;
            while (position < establishments.Length)
            {
                var batch = establishments
                    .Skip(position)
                    .Take(batchSize)
                    .Select(e=>e.Urn)
                    .ToArray();
                
                _logger.Debug(
                    $"Queuing {position} to {position + batch.Length} for processing");
                await _establishmentProcessingQueue.EnqueueBatchOfStagingAsync(batch, cancellationToken);

                position += batchSize;
            }

            _logger.Info("Finished downloading Establishments to cache");
        }

        public async Task ProcessBatchOfEstablishments(long[] urns, CancellationToken cancellationToken)
        {
            foreach (var urn in urns)
            {
                var current = await _establishmentRepository.GetEstablishmentAsync(urn, cancellationToken);
                var staging = await _establishmentRepository.GetEstablishmentFromStagingAsync(urn, cancellationToken);

                if (current == null)
                {
                    _logger.Info($"{urn} has not been seen before. Processing as created");

                    await ProcessEstablishment(staging, _eventPublisher.PublishLearningProviderCreatedAsync,
                        cancellationToken);
                }
                else if (!AreSame(current, staging))
                {
                    _logger.Info($"{urn} has changed. Processing as updated");

                    await ProcessEstablishment(staging, _eventPublisher.PublishLearningProviderUpdatedAsync,
                        cancellationToken);
                }
                else
                {
                    _logger.Info($"{urn} has not changed. Skipping");
                }
            }
        }

        private bool AreSame(Establishment current, Establishment staging)
        {
            if (current.Name != staging.Name)
            {
                return false;
            }

            return true;
        }

        private async Task ProcessEstablishment(Establishment staging,
            Func<LearningProvider, CancellationToken, Task> publishEvent,
            CancellationToken cancellationToken)
        {
            await _establishmentRepository.StoreAsync(staging, cancellationToken);
            _logger.Debug($"Stored {staging.Urn} in repository");

            var learningProvider = await _mapper.MapAsync<LearningProvider>(staging, cancellationToken);
            await publishEvent(learningProvider, cancellationToken);
            _logger.Debug($"Sent event for {staging.Urn}");
        }
    }
}