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
    public class ProcessStagingGroup
    {
        private const string FunctionName = nameof(ProcessStagingGroup);

        private readonly ICacheManager _cacheManager;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly ILoggerWrapper _logger;

        public ProcessStagingGroup(ICacheManager cacheManager, IHttpSpiExecutionContextManager httpSpiExecutionContextManager, ILoggerWrapper logger)
        {
            _cacheManager = cacheManager;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
            _logger = logger;
        }
        
        [StorageAccount("SPI_Cache:ProcessingQueueConnectionString")]
        [FunctionName(FunctionName)]
        public async Task Run(
            [QueueTrigger(CacheQueueNames.GroupProcessingQueue)]
            string queueContent, 
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());

            _logger.Info($"{FunctionName} trigger with: {queueContent}");

            var queueItem = JsonConvert.DeserializeObject<StagingBatchQueueItem<long>>(queueContent);
            _logger.Debug($"Deserialized to {queueItem.Urns.Length} urns for group {queueItem.ParentIdentifier} on {queueItem.PointInTime}");

            await _cacheManager.ProcessGroupAsync(queueItem.ParentIdentifier, queueItem.Urns, queueItem.PointInTime, cancellationToken);
        }
    }
}