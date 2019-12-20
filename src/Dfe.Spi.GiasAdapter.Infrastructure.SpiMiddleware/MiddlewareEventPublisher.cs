using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.Models;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Infrastructure.SpiMiddleware
{
    public class MiddlewareEventPublisher : IEventPublisher
    {
        private readonly ILoggerWrapper _logger;

        public MiddlewareEventPublisher(ILoggerWrapper logger)
        {
            _logger = logger;
        }
        
        public async Task PublishLearningProviderCreatedAsync(LearningProvider learningProvider, CancellationToken cancellationToken)
        {
            _logger.Debug($"Publish learning provider created: {JsonConvert.SerializeObject(learningProvider)}");
        }

        public async Task PublishLearningProviderUpdatedAsync(LearningProvider learningProvider, CancellationToken cancellationToken)
        {
            _logger.Debug($"Publish learning provider updated: {JsonConvert.SerializeObject(learningProvider)}");
        }
    }
}