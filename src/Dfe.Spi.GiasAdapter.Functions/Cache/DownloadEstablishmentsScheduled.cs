using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Microsoft.Azure.WebJobs;

namespace Dfe.Spi.GiasAdapter.Functions.Cache
{
    public class DownloadEstablishmentsScheduled
    {
        private const string FunctionName = nameof(DownloadEstablishmentsScheduled);
        private const string ScheduleExpression = "%SPI_Cache:EstablishmentSchedule%";

        private readonly ICacheManager _cacheManager;
        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly ILoggerWrapper _logger;

        public DownloadEstablishmentsScheduled(ICacheManager cacheManager, IHttpSpiExecutionContextManager httpSpiExecutionContextManager, ILoggerWrapper logger)
        {
            _cacheManager = cacheManager;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
            _logger = logger;
        }
        
        [FunctionName(FunctionName)]
        public async Task Run([TimerTrigger(ScheduleExpression)] TimerInfo timerInfo, CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetInternalRequestId(Guid.NewGuid());

            _logger.Info($"{FunctionName} started at {DateTime.UtcNow}. Past due: {timerInfo.IsPastDue}");

            await _cacheManager.DownloadEstablishmentsToCacheAsync(cancellationToken);
        }
    }
}