using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models;
using Dfe.Spi.Models.Entities;
using Dfe.Spi.Models.Extensions;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Application.LearningProviders
{
    public interface ILearningProviderManager
    {
        Task<LearningProvider> GetLearningProviderAsync(string id, string fields, bool readFromLive, DateTime? pointInTime, CancellationToken cancellationToken);
        Task<LearningProvider[]> GetLearningProvidersAsync(string[] ids, string[] fields, bool readFromLive, CancellationToken cancellationToken);
    }

    public class LearningProviderManager : ILearningProviderManager
    {
        private readonly IGiasApiClient _giasApiClient;
        private readonly IEstablishmentRepository _establishmentRepository;
        private readonly IMapper _mapper;
        private readonly ILoggerWrapper _logger;

        public LearningProviderManager(
            IGiasApiClient giasApiClient,
            IEstablishmentRepository establishmentRepository,
            IMapper mapper,
            ILoggerWrapper logger)
        {
            _giasApiClient = giasApiClient;
            _establishmentRepository = establishmentRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<LearningProvider> GetLearningProviderAsync(string id, string fields, bool readFromLive, DateTime? pointInTime, CancellationToken cancellationToken)
        {
            var establishment = readFromLive
                ? await GetEstablishmentFromApiAsync(id, pointInTime, cancellationToken)
                : await GetEstablishmentFromCacheAsync(id, pointInTime, cancellationToken);
            if (establishment == null)
            {
                return null;
            }

            _logger.Debug($"read establishment {id} from {(readFromLive ? "live" : "cache")}: {JsonConvert.SerializeObject(establishment)}");

            return await GetLearningProviderFromEstablishment(establishment, fields, cancellationToken);
        }

        public async Task<LearningProvider[]> GetLearningProvidersAsync(string[] ids, string[] fields, bool readFromLive, CancellationToken cancellationToken)
        {
            var pointInTime = (DateTime?) null;
            var establishments = readFromLive
                ? await GetEstablishmentsAsync(ids, pointInTime, GetEstablishmentFromApiAsync, cancellationToken)
                : await GetEstablishmentsAsync(ids, pointInTime, GetEstablishmentFromCacheAsync, cancellationToken);

            var fieldsString = fields == null || fields.Length == 0 ? null : fields.Aggregate((x, y) => $"{x},{y}");
            var providers = new LearningProvider[establishments.Length];

            for (var i = 0; i < establishments.Length; i++)
            {
                if (establishments[i] == null)
                {
                    continue;
                }

                providers[i] = await GetLearningProviderFromEstablishment(establishments[i], fieldsString, cancellationToken);
            }

            return providers;
        }


        private async Task<Establishment[]> GetEstablishmentsAsync(
            string[] ids,
            DateTime? pointInTime,
            Func<string, DateTime?, CancellationToken, Task<Establishment>> readerFunc,
            CancellationToken cancellationToken)
        {
            var tasks = new Task<Establishment[]>[5];
            var batchSize = (int) Math.Ceiling(ids.Length / (float) tasks.Length);
            for (var i = 0; i < tasks.Length; i++)
            {
                var batch = ids.Skip(i * batchSize).Take(batchSize).ToArray();
                tasks[i] = GetBatchOfEstablishmentsAsync(batch, pointInTime, readerFunc, cancellationToken);
            }

            var taskResults = await Task.WhenAll(tasks);
            var establishments = taskResults.SelectMany(x => x).ToArray();

            return establishments;
        }

        private async Task<Establishment[]> GetBatchOfEstablishmentsAsync(
            string[] batch,
            DateTime? pointInTime,
            Func<string, DateTime?, CancellationToken, Task<Establishment>> readerFunc,
            CancellationToken cancellationToken)
        {
            var establishments = new Establishment[batch.Length];

            for (var i = 0; i < batch.Length; i++)
            {
                var id = batch[i];

                establishments[i] = await readerFunc(id, pointInTime, cancellationToken);
            }

            return establishments;
        }

        private async Task<Establishment> GetEstablishmentFromApiAsync(string id, DateTime? pointInTime, CancellationToken cancellationToken)
        {
            if (!int.TryParse(id, out var urn))
            {
                throw new ArgumentException($"id must be a number (urn) but received {id}", nameof(id));
            }

            var establishment = await _giasApiClient.GetEstablishmentAsync(urn, cancellationToken);

            return establishment;
        }

        private async Task<Establishment> GetEstablishmentFromCacheAsync(string id, DateTime? pointInTime, CancellationToken cancellationToken)
        {
            if (!int.TryParse(id, out var urn))
            {
                throw new ArgumentException($"id must be a number (urn) but received {id}", nameof(id));
            }

            var establishment = await _establishmentRepository.GetEstablishmentAsync(urn, pointInTime, cancellationToken);

            return establishment;
        }

        private async Task<LearningProvider> GetLearningProviderFromEstablishment(Establishment establishment, string fields,
            CancellationToken cancellationToken)
        {
            var learningProvider = await _mapper.MapAsync<LearningProvider>(establishment, cancellationToken);
            _logger.Info($"mapped establishment {establishment.Urn} to {JsonConvert.SerializeObject(learningProvider)}");

            // If the fields are specified, then limit them... otherwise,
            // just return everything.
            if (!string.IsNullOrEmpty(fields))
            {
                learningProvider = learningProvider.Pick(fields);

                _logger.Debug(
                    $"Pruned mapped establishment: {learningProvider}.");
            }
            else
            {
                _logger.Debug("No fields specified - model not pruned.");
            }

            return learningProvider;
        }
    }
}