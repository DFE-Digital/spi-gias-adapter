using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Context.Models;
using Dfe.Spi.Common.Http.Client;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Newtonsoft.Json;
using RestSharp;

namespace Dfe.Spi.GiasAdapter.Infrastructure.SpiTranslator
{
    public class TranslatorApiClient : ITranslator
    {
        private readonly IRestClient _restClient;
        private readonly ISpiExecutionContextManager _spiExecutionContextManager;
        private readonly ILoggerWrapper _logger;
        private readonly Dictionary<string, Dictionary<string, string[]>> _cache;

        public TranslatorApiClient(
            IRestClient restClient,
            ISpiExecutionContextManager spiExecutionContextManager,
            TranslatorConfiguration configuration,
            ILoggerWrapper logger)
        {
            _restClient = restClient;
            _spiExecutionContextManager = spiExecutionContextManager;

            _restClient.BaseUrl = new Uri(configuration.BaseUrl);
            if (!string.IsNullOrEmpty(configuration.SubscriptionKey))
            {
                _restClient.DefaultParameters.Add(
                    new Parameter("Ocp-Apim-Subscription-Key", configuration.SubscriptionKey, ParameterType.HttpHeader));
            }

            _logger = logger;

            _cache = new Dictionary<string, Dictionary<string, string[]>>();
        }

        public async Task<string> TranslateEnumValue(string enumName, string sourceValue,
            CancellationToken cancellationToken)
        {
            var mappings = await GetMappings(enumName, sourceValue, cancellationToken);
            var mapping = mappings.FirstOrDefault(kvp =>
                kvp.Value.Any(v => v.Equals(sourceValue, StringComparison.InvariantCultureIgnoreCase))).Key;
            if (string.IsNullOrEmpty(mapping))
            {
                _logger.Info($"No enum mapping found for GIAS for {enumName} with value {sourceValue}");
                return null;
            }
            
            _logger.Debug($"Found mapping of {mapping} for {enumName} with value {sourceValue}");
            return mapping;
        }

        private async Task<Dictionary<string, string[]>> GetMappings(string enumName, string sourceValue,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"{enumName}:{sourceValue}";
            if (_cache.ContainsKey(cacheKey))
            {
                return _cache[cacheKey];
            }

            var mappings = await GetMappingsFromApi(enumName, sourceValue, cancellationToken);
            _cache.Add(cacheKey, mappings);
            return mappings;
        }

        private async Task<Dictionary<string, string[]>> GetMappingsFromApi(string enumName, string sourceValue,
            CancellationToken cancellationToken)
        {
            var resource = $"enumerations/{enumName}/{SourceSystemNames.GetInformationAboutSchools}";
            _logger.Info($"Calling {resource} on translator api");
            var request = new RestRequest(resource, Method.GET);

            SpiExecutionContext spiExecutionContext =
                _spiExecutionContextManager.SpiExecutionContext;

            request.AppendContext(spiExecutionContext);

            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            if (!response.IsSuccessful)
            {
                throw new TranslatorApiException(resource, response.StatusCode, response.Content);
            }

            _logger.Info($"Received {response.Content}");
            var translationResponse = JsonConvert.DeserializeObject<TranslationResponse>(response.Content);
            return translationResponse.MappingsResult.Mappings;
        }
    }

    internal class TranslationResponse
    {
        public TranslationMappingsResult MappingsResult { get; set; }
    }

    internal class TranslationMappingsResult
    {
        public Dictionary<string, string[]> Mappings { get; set; }
    }
}