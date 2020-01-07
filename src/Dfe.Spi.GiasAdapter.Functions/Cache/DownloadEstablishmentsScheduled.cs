using System;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILoggerWrapper _logger;

        public DownloadEstablishmentsScheduled(ICacheManager cacheManager, ILoggerWrapper logger)
        {
            _cacheManager = cacheManager;
            _logger = logger;
        }
        
        [FunctionName(FunctionName)]
        public async Task Run([TimerTrigger(ScheduleExpression)] TimerInfo timerInfo, CancellationToken cancellationToken)
        {
            _logger.SetInternalRequestId(Guid.NewGuid());
            _logger.Info($"{FunctionName} started at {DateTime.UtcNow}. Past due: {timerInfo.IsPastDue}");

            await _cacheManager.DownloadEstablishmentsToCacheAsync(cancellationToken);
        }
    }
}