using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.Models.Entities;
using Newtonsoft.Json;
using RestSharp;

namespace Dfe.Spi.GiasAdapter.Infrastructure.SpiMiddleware
{
    public class MiddlewareEventPublisher : IEventPublisher
    {
        private readonly IRestClient _restClient;
        private readonly ILoggerWrapper _logger;

        public MiddlewareEventPublisher(MiddlewareConfiguration configuration, IRestClient restClient,
            ILoggerWrapper logger)
        {
            _restClient = restClient;
            _restClient.BaseUrl = new Uri(configuration.BaseUrl, UriKind.Absolute);
            if (!string.IsNullOrEmpty(configuration.SubscriptionKey))
            {
                _restClient.DefaultParameters.Add(new Parameter("Ocp-Apim-Subscription-Key", configuration.SubscriptionKey,
                    ParameterType.HttpHeader));
            }

            _logger = logger;
        }

        
        public async Task PublishLearningProviderCreatedAsync(LearningProvider learningProvider,
            CancellationToken cancellationToken)
        {
            await SendEventToMiddleware("learning-provider-created", learningProvider, cancellationToken);
            _logger.Debug($"Published learning provider created: {JsonConvert.SerializeObject(learningProvider)}");
        }

        public async Task PublishLearningProviderUpdatedAsync(LearningProvider learningProvider,
            CancellationToken cancellationToken)
        {
            await SendEventToMiddleware("learning-provider-updated", learningProvider, cancellationToken);
            _logger.Debug($"Published learning provider updated: {JsonConvert.SerializeObject(learningProvider)}");
        }

        
        public async Task PublishManagementGroupCreatedAsync(ManagementGroup managementGroup,
            CancellationToken cancellationToken)
        {
            await SendEventToMiddleware("management-group-created", managementGroup, cancellationToken);
            _logger.Debug($"Published management group created: {JsonConvert.SerializeObject(managementGroup)}");
        }

        public async Task PublishManagementGroupUpdatedAsync(ManagementGroup managementGroup,
            CancellationToken cancellationToken)
        {
            await SendEventToMiddleware("management-group-updated", managementGroup, cancellationToken);
            _logger.Debug($"Published management group updated: {JsonConvert.SerializeObject(managementGroup)}");
        }
        

        private async Task SendEventToMiddleware(string eventType, object details, CancellationToken cancellationToken)
        {
            var request = new RestRequest(eventType, Method.POST, DataFormat.Json);
            request.AddParameter(string.Empty, JsonConvert.SerializeObject(details), ParameterType.RequestBody);
            
            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            if (!response.IsSuccessful)
            {
                throw new MiddlewareException(eventType, response.StatusCode, response.Content);
            }
        }
    }
}