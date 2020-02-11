using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Functions.Cache;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Functions.UnitTests.Cache
{
    public class WhenDownloadingEstablishmentsOnSchedule
    {
        private Mock<ICacheManager> _cacheManagerMock;
        private Mock<IHttpSpiExecutionContextManager> _httpSpiExecutionContextManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private DownloadEstablishmentsScheduled _function;
        private TimerInfo _timerInfo;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _cacheManagerMock = new Mock<ICacheManager>();

            _httpSpiExecutionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _function = new DownloadEstablishmentsScheduled(
                _cacheManagerMock.Object,
                _httpSpiExecutionContextManagerMock.Object,
                _loggerMock.Object);

            _timerInfo = new TimerInfo(new ConstantSchedule(
                    new TimeSpan()),
                new ScheduleStatus());

            _cancellationToken = default(CancellationToken);
        }

        [Test]
        public async Task ThenItShouldDownloadEstablishmentsToCache()
        {
            await _function.Run(_timerInfo, _cancellationToken);

            _cacheManagerMock.Verify(m => m.DownloadEstablishmentsToCacheAsync(_cancellationToken), Times.Once);
        }
    }
}