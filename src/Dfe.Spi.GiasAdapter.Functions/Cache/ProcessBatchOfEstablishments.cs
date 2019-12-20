using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Functions.Cache
{
    public class ProcessBatchOfEstablishments
    {
        private const string FunctionName = nameof(ProcessBatchOfEstablishments);

        private readonly ICacheManager _cacheManager;
        private readonly ILoggerWrapper _logger;

        public ProcessBatchOfEstablishments(ICacheManager cacheManager, ILoggerWrapper logger)
        {
            _cacheManager = cacheManager;
            _logger = logger;
        }
        
        [StorageAccount("SPI_Cache:EstablishmentProcessingQueueConnectionString")]
        [FunctionName(FunctionName)]
        public async Task Run(
            [QueueTrigger(CacheQueueNames.EstablishmentProcessingQueue)]
            string queueContent, 
            CancellationToken cancellationToken)
        {
            _logger.SetInternalRequestId(Guid.NewGuid());
            _logger.Info($"{FunctionName} trigger with: {queueContent}");

            var urns = JsonConvert.DeserializeObject<long[]>(queueContent);
            _logger.Debug($"Deserialized to {urns.Length} urns");

            await _cacheManager.ProcessBatchOfEstablishments(urns, cancellationToken);
        }
    }
}