using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
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
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly ILoggerWrapper _logger;

        public ProcessBatchOfEstablishments(ICacheManager cacheManager, IHttpSpiExecutionContextManager httpSpiExecutionContextManager, ILoggerWrapper logger)
        {
            _cacheManager = cacheManager;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
            _logger = logger;
        }
        
        [StorageAccount("SPI_Cache:ProcessingQueueConnectionString")]
        [FunctionName(FunctionName)]
        public async Task Run(
            [QueueTrigger(CacheQueueNames.EstablishmentProcessingQueue)]
            string queueContent, 
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());

            _logger.Info($"{FunctionName} trigger with: {queueContent}");

            var queueItem = JsonConvert.DeserializeObject<StagingBatchQueueItem<long>>(queueContent);
            _logger.Debug($"Deserialized to {queueItem.Urns.Length} urns on {queueItem.PointInTime}");

            // await _cacheManager.ProcessBatchOfEstablishments(queueItem.Identifiers, queueItem.PointInTime, cancellationToken);
            throw new Exception("No longer used"); // TODO: Delete this function
        }
    }
}