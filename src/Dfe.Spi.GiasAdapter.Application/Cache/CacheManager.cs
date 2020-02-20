using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models.Entities;

namespace Dfe.Spi.GiasAdapter.Application.Cache
{
    public interface ICacheManager
    {
        Task DownloadAllGiasDataToCacheAsync(CancellationToken cancellationToken);
        Task ProcessBatchOfEstablishments(long[] urns, CancellationToken cancellationToken);
        Task ProcessBatchOfGroups(long[] uids, CancellationToken cancellationToken);
    }

    public class CacheManager : ICacheManager
    {
        private readonly IGiasApiClient _giasApiClient;
        private readonly IEstablishmentRepository _establishmentRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;
        private readonly IEventPublisher _eventPublisher;
        private readonly IEstablishmentProcessingQueue _establishmentProcessingQueue;
        private readonly IGroupProcessingQueue _groupProcessingQueue;
        private readonly ILoggerWrapper _logger;

        public CacheManager(
            IGiasApiClient giasApiClient,
            IEstablishmentRepository establishmentRepository,
            IGroupRepository groupRepository,
            IMapper mapper,
            IEventPublisher eventPublisher,
            IEstablishmentProcessingQueue establishmentProcessingQueue,
            IGroupProcessingQueue groupProcessingQueue,
            ILoggerWrapper logger)
        {
            _giasApiClient = giasApiClient;
            _establishmentRepository = establishmentRepository;
            _groupRepository = groupRepository;
            _mapper = mapper;
            _eventPublisher = eventPublisher;
            _establishmentProcessingQueue = establishmentProcessingQueue;
            _groupProcessingQueue = groupProcessingQueue;
            _logger = logger;
        }


        public async Task DownloadAllGiasDataToCacheAsync(CancellationToken cancellationToken)
        {
            var groupLinks = await _giasApiClient.DownloadGroupLinksAsync(cancellationToken);

            await DownloadGroupsToCacheAsync(cancellationToken);
            await DownloadEstablishmentsToCacheAsync(groupLinks, cancellationToken);
        }

        public async Task ProcessBatchOfEstablishments(long[] urns, CancellationToken cancellationToken)
        {
            foreach (var urn in urns)
            {
                var current = await _establishmentRepository.GetEstablishmentAsync(urn, cancellationToken);
                var staging = await _establishmentRepository.GetEstablishmentFromStagingAsync(urn, cancellationToken);

                if (current == null)
                {
                    _logger.Info($"Establishment {urn} has not been seen before. Processing as created");

                    await ProcessEstablishment(staging, _eventPublisher.PublishLearningProviderCreatedAsync,
                        cancellationToken);
                }
                else if (!AreSame(current, staging))
                {
                    _logger.Info($"Establishment {urn} has changed. Processing as updated");

                    await ProcessEstablishment(staging, _eventPublisher.PublishLearningProviderUpdatedAsync,
                        cancellationToken);
                }
                else
                {
                    _logger.Info($"Establishment {urn} has not changed. Skipping");
                }
            }
        }

        public async Task ProcessBatchOfGroups(long[] uids, CancellationToken cancellationToken)
        {
            foreach (var uid in uids)
            {
                var current = await _groupRepository.GetGroupAsync(uid, cancellationToken);
                var staging = await _groupRepository.GetGroupFromStagingAsync(uid, cancellationToken);

                if (current == null)
                {
                    _logger.Info($"Group {uid} has not been seen before. Processing as created");

                    await ProcessGroup(staging, _eventPublisher.PublishManagementGroupCreatedAsync,
                        cancellationToken);
                }
                else if (!AreSame(current, staging))
                {
                    _logger.Info($"Group {uid} has changed. Processing as updated");

                    await ProcessGroup(staging, _eventPublisher.PublishManagementGroupUpdatedAsync,
                        cancellationToken);
                }
                else
                {
                    _logger.Info($"Group {uid} has not changed. Skipping");
                }
            }
        }


        private async Task DownloadGroupsToCacheAsync(CancellationToken cancellationToken)
        {
            _logger.Info("Acquiring groups file from GIAS...");

            // Download
            var groups = await _giasApiClient.DownloadGroupsAsync(cancellationToken);
            _logger.Info($"Downloaded {groups.Length} groups from GIAS");

            // Store
            await _groupRepository.StoreInStagingAsync(groups, cancellationToken);
            _logger.Info($"Stored {groups.Length} groups in staging");

            // Queue diff check
            var position = 0;
            const int batchSize = 100;
            while (position < groups.Length)
            {
                var batch = groups
                    .Skip(position)
                    .Take(batchSize)
                    .Select(e => e.Uid)
                    .ToArray();

                _logger.Debug(
                    $"Queuing {position} to {position + batch.Length} of groups for processing");
                await _groupProcessingQueue.EnqueueBatchOfStagingAsync(batch, cancellationToken);

                position += batchSize;
            }

            _logger.Info("Finished downloading groups to cache");
        }

        private async Task DownloadEstablishmentsToCacheAsync(GroupLink[] groupLinks,
            CancellationToken cancellationToken)
        {
            _logger.Info("Acquiring establishments file from GIAS...");

            // Download
            var establishments = await _giasApiClient.DownloadEstablishmentsAsync(cancellationToken);
            _logger.Info($"Downloaded {establishments.Length} establishments from GIAS");

            // Add links
            foreach (var establishment in establishments)
            {
                establishment.GroupLinks = groupLinks.Where(l => l.Urn == establishment.Urn).ToArray();
                _logger.Debug($"Added {establishment.GroupLinks.Length} links to establishment {establishment.Urn}");
            }

            // Store
            await _establishmentRepository.StoreInStagingAsync(establishments, cancellationToken);
            _logger.Info($"Stored {establishments.Length} establishments in staging");

            // Queue diff check
            var position = 0;
            const int batchSize = 100;
            while (position < establishments.Length)
            {
                var batch = establishments
                    .Skip(position)
                    .Take(batchSize)
                    .Select(e => e.Urn)
                    .ToArray();

                _logger.Debug(
                    $"Queuing {position} to {position + batch.Length} of establishments for processing");
                await _establishmentProcessingQueue.EnqueueBatchOfStagingAsync(batch, cancellationToken);

                position += batchSize;
            }

            _logger.Info("Finished downloading Establishments to cache");
        }

        private bool AreSame(Establishment current, Establishment staging)
        {
            if (current.EstablishmentName != staging.EstablishmentName)
            {
                return false;
            }

            if (current.Ukprn != staging.Ukprn)
            {
                return false;
            }

            if (current.Uprn != staging.Uprn)
            {
                return false;
            }

            if (current.CompaniesHouseNumber != staging.CompaniesHouseNumber)
            {
                return false;
            }

            if (current.CharitiesCommissionNumber != staging.CharitiesCommissionNumber)
            {
                return false;
            }

            if (current.Trusts?.Code != staging.Trusts?.Code)
            {
                return false;
            }

            if (current.LA?.Code != staging.LA?.Code)
            {
                return false;
            }

            if (current.EstablishmentNumber != staging.EstablishmentNumber)
            {
                return false;
            }

            if (current.PreviousEstablishmentNumber != staging.PreviousEstablishmentNumber)
            {
                return false;
            }

            if (current.Postcode != staging.Postcode)
            {
                return false;
            }

            return true;
        }

        private bool AreSame(Group current, Group staging)
        {
            return current.GroupName == staging.GroupName &&
                   current.GroupType == staging.GroupType &&
                   current.CompaniesHouseNumber == staging.CompaniesHouseNumber;
        }

        private async Task ProcessEstablishment(Establishment staging,
            Func<LearningProvider, CancellationToken, Task> publishEvent,
            CancellationToken cancellationToken)
        {
            await _establishmentRepository.StoreAsync(staging, cancellationToken);
            _logger.Debug($"Stored establishment {staging.Urn} in repository");

            var learningProvider = await _mapper.MapAsync<LearningProvider>(staging, cancellationToken);
            learningProvider._Lineage = null;
            await publishEvent(learningProvider, cancellationToken);
            _logger.Debug($"Sent event for establishment {staging.Urn}");
        }

        private async Task ProcessGroup(Group staging,
            Func<ManagementGroup, CancellationToken, Task> publishEvent,
            CancellationToken cancellationToken)
        {
            await _groupRepository.StoreAsync(staging, cancellationToken);
            _logger.Debug($"Stored group {staging.Uid} in repository");

            var managementGroup = await _mapper.MapAsync<ManagementGroup>(staging, cancellationToken);
            managementGroup._Lineage = null;
            await publishEvent(managementGroup, cancellationToken);
            _logger.Debug($"Sent event for group {staging.Uid}");
        }
    }
}