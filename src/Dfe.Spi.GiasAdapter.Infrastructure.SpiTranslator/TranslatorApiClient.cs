﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Caching.Definitions;
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
        private readonly OAuth2ClientCredentialsAuthenticator _oAuth2ClientCredentialsAuthenticator;
        private readonly IRestClient _restClient;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpiExecutionContextManager _spiExecutionContextManager;
        private readonly ILoggerWrapper _logger;

        public TranslatorApiClient(
            AuthenticationConfiguration authenticationConfiguration,
            IRestClient restClient,
            ICacheProvider cacheProvider,
            ISpiExecutionContextManager spiExecutionContextManager,
            TranslatorConfiguration configuration,
            ILoggerWrapper logger)
        {
            _restClient = restClient;
            _cacheProvider = cacheProvider;
            _spiExecutionContextManager = spiExecutionContextManager;

            _restClient.BaseUrl = new Uri(configuration.BaseUrl);
            if (!string.IsNullOrEmpty(configuration.SubscriptionKey))
            {
                _restClient.DefaultParameters.Add(
                    new Parameter("Ocp-Apim-Subscription-Key", configuration.SubscriptionKey, ParameterType.HttpHeader));
            }

            _logger = logger;

            _oAuth2ClientCredentialsAuthenticator = new OAuth2ClientCredentialsAuthenticator(
                authenticationConfiguration.TokenEndpoint,
                authenticationConfiguration.ClientId,
                authenticationConfiguration.ClientSecret,
                authenticationConfiguration.Resource);
        }

        public async Task<string> TranslateEnumValue(string enumName, string sourceValue,
            CancellationToken cancellationToken)
        {
            var mappings = await GetMappings(enumName, cancellationToken);
            if (mappings == null)
            {
                return null;
            }
            
            var mapping = mappings.FirstOrDefault(kvp =>
                kvp.Value.Any(v => v.Equals(sourceValue, StringComparison.InvariantCultureIgnoreCase))).Key;
            if (string.IsNullOrEmpty(mapping))
            {
                _logger.Warning($"No enum mapping found for GIAS for {enumName} with value {sourceValue}");
                return null;
            }
            
            _logger.Debug($"Found mapping of {mapping} for {enumName} with value {sourceValue}");
            return mapping;
        }

        private async Task<Dictionary<string, string[]>> GetMappings(string enumName, CancellationToken cancellationToken)
        {
            var allMappings = await GetMappings(cancellationToken);
            if (allMappings == null)
            {
                return null;
            }
            
            var key = allMappings.Keys.SingleOrDefault(mappingsKey => mappingsKey.Equals(enumName, StringComparison.InvariantCultureIgnoreCase));
            return string.IsNullOrEmpty(key)
                ? null
                : allMappings[key];
        }

        private async Task<Dictionary<string, Dictionary<string, string[]>>> GetMappings(CancellationToken cancellationToken)
        {
            const string cacheKey = "AllEnumMappings";

            var cached = (Dictionary<string, Dictionary<string, string[]>>)(await _cacheProvider.GetCacheItemAsync(cacheKey, cancellationToken));
            if (cached != null)
            {
                return cached;
            }
            
            var mappings = await GetMappingsFromApi(cancellationToken);

            if (mappings != null)
            {
                await _cacheProvider.AddCacheItemAsync(cacheKey, mappings,
                    new TimeSpan(0, 1, 0), cancellationToken);
            }

            return mappings;
        }

        private async Task<Dictionary<string, Dictionary<string, string[]>>> GetMappingsFromApi(CancellationToken cancellationToken)
        {
            var resource = $"adapters/{SourceSystemNames.GetInformationAboutSchools}/mappings";
            _logger.Info($"Calling {resource} on translator api");
            var request = new RestRequest(resource, Method.GET);

            SpiExecutionContext spiExecutionContext =
                _spiExecutionContextManager.SpiExecutionContext;

            request.AppendContext(spiExecutionContext);

            // Do we have an OAuth token?
            // Or is this a server process?
            string identityToken = spiExecutionContext.IdentityToken;

            if (string.IsNullOrEmpty(identityToken))
            {
                _logger.Debug(
                    $"No OAuth token present in the " +
                    $"{nameof(SpiExecutionContext)}. The " +
                    $"{nameof(OAuth2ClientCredentialsAuthenticator)} will " +
                    $"be used, and a new token generated.");

                // The fact we don't have a token means this is probably a
                // server process.
                // We need to generate one...
                _restClient.Authenticator = _oAuth2ClientCredentialsAuthenticator;
            }
            else
            {
                _logger.Debug(
                    $"OAuth token present in the " +
                    $"{nameof(SpiExecutionContext)}. This will be used in " +
                    $"calling the Translator.");
            }

            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            if (!response.IsSuccessful)
            {
                throw new TranslatorApiException(
                    resource,
                    response.StatusCode,
                    response.Content,
                    response.ErrorException);
            }

            _logger.Debug($"Received {response.Content}");
            var translationResponse = JsonConvert.DeserializeObject<Dictionary<string, TranslationMappingsResult>>(response.Content);
            return translationResponse
                .Select(kvp =>
                    new
                    {
                        EnumerationName = kvp.Key,
                        Mappings = kvp.Value.Mappings,
                    })
                .ToDictionary(
                    x => x.EnumerationName,
                    x => x.Mappings);
        }
    }
}