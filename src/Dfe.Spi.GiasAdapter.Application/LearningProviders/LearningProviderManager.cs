using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Logging.Definitions;
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
        Task<LearningProvider> GetLearningProviderAsync(string id, string fields, CancellationToken cancellationToken);
        Task<LearningProvider[]> GetLearningProvidersAsync(string[] ids, string[] fields, CancellationToken cancellationToken);
    }

    public class LearningProviderManager : ILearningProviderManager
    {
        private readonly IGiasApiClient _giasApiClient;
        private readonly IMapper _mapper;
        private readonly ILoggerWrapper _logger;

        public LearningProviderManager(IGiasApiClient giasApiClient, IMapper mapper, ILoggerWrapper logger)
        {
            _giasApiClient = giasApiClient;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<LearningProvider> GetLearningProviderAsync(string id, string fields, CancellationToken cancellationToken)
        {
            int urn;
            if (!int.TryParse(id, out urn))
            {
                throw new ArgumentException($"id must be a number (urn) but received {id}", nameof(id));
            }

            var establishment = await _giasApiClient.GetEstablishmentAsync(urn, cancellationToken);
            if (establishment == null)
            {
                return null;
            }

            _logger.Info($"read establishment {urn}: {JsonConvert.SerializeObject(establishment)}");

            var learningProvider = await _mapper.MapAsync<LearningProvider>(establishment, cancellationToken);
            _logger.Info($"mapped establishment {urn} to {JsonConvert.SerializeObject(learningProvider)}");

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

        public async Task<LearningProvider[]> GetLearningProvidersAsync(string[] ids, string[] fields, CancellationToken cancellationToken)
        {
            var fieldsString = fields == null || fields.Length == 0 ? null : fields.Aggregate((x, y) => $"{x},{y}");

            var tasks = new Task<Establishment[]>[5];
            var batchSize = (int) Math.Ceiling(ids.Length / (float) tasks.Length);
            for (var i = 0; i < tasks.Length; i++)
            {
                var batch = ids.Skip(i * batchSize).Take(batchSize).ToArray();
                tasks[i] = GetBatchOfEstablishmentsAsync(batch, fieldsString, cancellationToken);
            }

            var taskResults = await Task.WhenAll(tasks);
            var establishments = taskResults.SelectMany(x => x).ToArray();
            var providers = new LearningProvider[establishments.Length];

            for (var i = 0; i < establishments.Length; i++)
            {
                if (establishments[i] == null)
                {
                    continue;
                }

                providers[i] = await _mapper.MapAsync<LearningProvider>(establishments[i], cancellationToken);
            }

            return providers;
        }

        private async Task<Establishment[]> GetBatchOfEstablishmentsAsync(string[] batch, string fields, CancellationToken cancellationToken)
        {
            var establishments = new Establishment[batch.Length];

            for (var i = 0; i < batch.Length; i++)
            {
                var id = batch[i];

                int urn;
                if (!int.TryParse(id, out urn))
                {
                    throw new ArgumentException($"id must be a number (urn) but received {id}", nameof(id));
                }

                establishments[i] = await _giasApiClient.GetEstablishmentAsync(urn, cancellationToken);
            }

            return establishments;
        }
    }
}