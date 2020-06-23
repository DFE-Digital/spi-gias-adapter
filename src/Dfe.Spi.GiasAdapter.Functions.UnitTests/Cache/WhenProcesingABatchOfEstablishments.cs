using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Functions.Cache;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Functions.UnitTests.Cache
{
    public class WhenProcesingABatchOfEstablishments
    {
        private Mock<ICacheManager> _cacheManagerMock;
        private Mock<IHttpSpiExecutionContextManager> _httpSpiExecutionContextManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private ProcessBatchOfEstablishments _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _cacheManagerMock = new Mock<ICacheManager>();

            _httpSpiExecutionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _function = new ProcessBatchOfEstablishments(
                _cacheManagerMock.Object,
                _httpSpiExecutionContextManagerMock.Object,
                _loggerMock.Object);

            _cancellationToken = default(CancellationToken);
        }

        [Test, AutoData]
        public async Task ThenItShouldCallCacheManagerWithDeserializedUrns(long[] urns, DateTime pointInTime)
        {
            var queueItem = new StagingBatchQueueItem<long>
            {
                Identifiers = urns,
                PointInTime = pointInTime,
            };
            
            await _function.Run(JsonConvert.SerializeObject(queueItem), _cancellationToken);

            _cacheManagerMock.Verify(m => m.ProcessBatchOfEstablishments(
                It.Is<long[]>(actual => AreEqual(urns, actual)),
                pointInTime,
                _cancellationToken), Times.Once);
        }


        private bool AreEqual(long[] expected, long[] actual)
        {
            // Null check
            if (expected == null && actual == null)
            {
                return true;
            }

            if (expected == null || actual == null)
            {
                return false;
            }

            // Length check
            if (expected.Length != actual.Length)
            {
                return false;
            }

            // Item check
            for (var i = 0; i < expected.Length; i++)
            {
                if (expected[i] != actual[i])
                {
                    return false;
                }
            }

            // All good
            return true;
        }
    }
}