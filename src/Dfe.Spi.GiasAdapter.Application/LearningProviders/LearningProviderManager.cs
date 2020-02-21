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
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Application.LearningProviders
{
    public interface ILearningProviderManager
    {
        Task<LearningProvider> GetLearningProviderAsync(string id, string fields, CancellationToken cancellationToken);
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

                _logger.Info(
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