using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Application.UnitTests.Cache
{
    public class WhenTidyingCache
    {
        private Mock<IGiasApiClient> _giasApiClientMock;
        private Mock<IStateRepository> _stateRepositoryMock;
        private Mock<IEstablishmentRepository> _establishmentRepositoryMock;
        private Mock<IGroupRepository> _groupRepositoryMock;
        private Mock<ILocalAuthorityRepository> _localAuthorityRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEventPublisher> _eventPublisherMock;
        private Mock<IEstablishmentProcessingQueue> _establishmentProcessingQueueMock;
        private Mock<IGroupProcessingQueue> _groupProcessingQueueMock;
        private Mock<ILocalAuthorityProcessingQueue> _localAuthorityProcessingQueueMock;
        private CacheConfiguration _configuration;
        private Mock<ILoggerWrapper> _loggerMock;
        private CacheManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _giasApiClientMock = new Mock<IGiasApiClient>();

            _stateRepositoryMock = new Mock<IStateRepository>();
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-15));
            
            _establishmentRepositoryMock = new Mock<IEstablishmentRepository>();

            _groupRepositoryMock = new Mock<IGroupRepository>();

            _localAuthorityRepositoryMock = new Mock<ILocalAuthorityRepository>();

            _mapperMock = new Mock<IMapper>();

            _eventPublisherMock = new Mock<IEventPublisher>();

            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();

            _groupProcessingQueueMock = new Mock<IGroupProcessingQueue>();

            _localAuthorityProcessingQueueMock = new Mock<ILocalAuthorityProcessingQueue>();

            _configuration = new CacheConfiguration
            {
                NumberOfDaysToRetainStagingData = 14,
            };

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new CacheManager(
                _giasApiClientMock.Object,
                _stateRepositoryMock.Object,
                _establishmentRepositoryMock.Object,
                _groupRepositoryMock.Object,
                _localAuthorityRepositoryMock.Object,
                _mapperMock.Object,
                _eventPublisherMock.Object,
                _establishmentProcessingQueueMock.Object,
                _groupProcessingQueueMock.Object,
                _localAuthorityProcessingQueueMock.Object,
                _configuration,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldGetLastClearedDateForEstabishmentsFromRepo()
        {
            await _manager.TidyCacheAsync(_cancellationToken);

            _stateRepositoryMock.Verify(r => r.GetLastStagingDateClearedAsync("establishments", _cancellationToken),
                Times.Once);
        }

        [TestCase(15, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 3)]
        [TestCase(18, 4)]
        public async Task ThenItShouldSetLastClearedStateOfEstablishmentsForEachDayBetweenLastClearedAndRetentionDate(
            int numberOfDaysAgoOfLastCleared,
            int expectedNumberOfDaysToClearInRun)
        {
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync("establishments", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-numberOfDaysAgoOfLastCleared));

            await _manager.TidyCacheAsync(_cancellationToken);

            _stateRepositoryMock.Verify(r => r.SetLastStagingDateClearedAsync("establishments", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedNumberOfDaysToClearInRun));
            for (var i = 1; i <= expectedNumberOfDaysToClearInRun; i++)
            {
                var expectedDate = DateTime.Today.AddDays(-(numberOfDaysAgoOfLastCleared - i));
                _stateRepositoryMock.Verify(r => r.SetLastStagingDateClearedAsync("establishments", expectedDate, _cancellationToken),
                    Times.Once, $"Did not set date for date {i} days ago");
            }
        }

        [TestCase(15, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 3)]
        [TestCase(18, 4)]
        public async Task ThenItShouldClearEstablishmentsStagingDataForEachDayBetweenLastClearedAndRetentionDate(
            int numberOfDaysAgoOfLastCleared,
            int expectedNumberOfDaysToClearInRun)
        {
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync("establishments", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-numberOfDaysAgoOfLastCleared));

            await _manager.TidyCacheAsync(_cancellationToken);

            _establishmentRepositoryMock.Verify(r => r.ClearStagingDataForDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedNumberOfDaysToClearInRun));
            for (var i = 1; i <= expectedNumberOfDaysToClearInRun; i++)
            {
                var expectedDate = DateTime.Today.AddDays(-(numberOfDaysAgoOfLastCleared - i));
                _establishmentRepositoryMock.Verify(r => r.ClearStagingDataForDateAsync(expectedDate, _cancellationToken),
                    Times.Once, $"Did not clear data for date {i} days ago");
            }
        }

        [Test]
        public async Task ThenItShouldGetLastClearedDateForGroupsFromRepo()
        {
            await _manager.TidyCacheAsync(_cancellationToken);

            _stateRepositoryMock.Verify(r => r.GetLastStagingDateClearedAsync("groups", _cancellationToken),
                Times.Once);
        }

        [TestCase(15, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 3)]
        [TestCase(18, 4)]
        public async Task ThenItShouldSetLastClearedStateOfGroupsForEachDayBetweenLastClearedAndRetentionDate(
            int numberOfDaysAgoOfLastCleared,
            int expectedNumberOfDaysToClearInRun)
        {
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync("groups", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-numberOfDaysAgoOfLastCleared));

            await _manager.TidyCacheAsync(_cancellationToken);

            _stateRepositoryMock.Verify(r => r.SetLastStagingDateClearedAsync("groups", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedNumberOfDaysToClearInRun));
            for (var i = 1; i <= expectedNumberOfDaysToClearInRun; i++)
            {
                var expectedDate = DateTime.Today.AddDays(-(numberOfDaysAgoOfLastCleared - i));
                _stateRepositoryMock.Verify(r => r.SetLastStagingDateClearedAsync("groups", expectedDate, _cancellationToken),
                    Times.Once, $"Did not set date for date {i} days ago");
            }
        }

        [TestCase(15, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 3)]
        [TestCase(18, 4)]
        public async Task ThenItShouldClearGroupsStagingDataForEachDayBetweenLastClearedAndRetentionDate(
            int numberOfDaysAgoOfLastCleared,
            int expectedNumberOfDaysToClearInRun)
        {
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync("groups", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-numberOfDaysAgoOfLastCleared));

            await _manager.TidyCacheAsync(_cancellationToken);

            _groupRepositoryMock.Verify(r => r.ClearStagingDataForDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedNumberOfDaysToClearInRun));
            for (var i = 1; i <= expectedNumberOfDaysToClearInRun; i++)
            {
                var expectedDate = DateTime.Today.AddDays(-(numberOfDaysAgoOfLastCleared - i));
                _groupRepositoryMock.Verify(r => r.ClearStagingDataForDateAsync(expectedDate, _cancellationToken),
                    Times.Once, $"Did not clear data for date {i} days ago");
            }
        }

        [Test]
        public async Task ThenItShouldGetLastClearedDateForLocalAuthoritiesFromRepo()
        {
            await _manager.TidyCacheAsync(_cancellationToken);

            _stateRepositoryMock.Verify(r => r.GetLastStagingDateClearedAsync("local-authorities", _cancellationToken),
                Times.Once);
        }

        [TestCase(15, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 3)]
        [TestCase(18, 4)]
        public async Task ThenItShouldSetLastClearedStateOfLocalAuthoritiesForEachDayBetweenLastClearedAndRetentionDate(
            int numberOfDaysAgoOfLastCleared,
            int expectedNumberOfDaysToClearInRun)
        {
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync("local-authorities", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-numberOfDaysAgoOfLastCleared));

            await _manager.TidyCacheAsync(_cancellationToken);

            _stateRepositoryMock.Verify(r => r.SetLastStagingDateClearedAsync("local-authorities", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedNumberOfDaysToClearInRun));
            for (var i = 1; i <= expectedNumberOfDaysToClearInRun; i++)
            {
                var expectedDate = DateTime.Today.AddDays(-(numberOfDaysAgoOfLastCleared - i));
                _stateRepositoryMock.Verify(r => r.SetLastStagingDateClearedAsync("local-authorities", expectedDate, _cancellationToken),
                    Times.Once, $"Did not set date for date {i} days ago");
            }
        }

        [TestCase(15, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 3)]
        [TestCase(18, 4)]
        public async Task ThenItShouldClearLocalAuthoritiesStagingDataForEachDayBetweenLastClearedAndRetentionDate(
            int numberOfDaysAgoOfLastCleared,
            int expectedNumberOfDaysToClearInRun)
        {
            _stateRepositoryMock.Setup(r => r.GetLastStagingDateClearedAsync("local-authorities", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DateTime.Today.AddDays(-numberOfDaysAgoOfLastCleared));

            await _manager.TidyCacheAsync(_cancellationToken);

            _localAuthorityRepositoryMock.Verify(r => r.ClearStagingDataForDateAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(expectedNumberOfDaysToClearInRun));
            for (var i = 1; i <= expectedNumberOfDaysToClearInRun; i++)
            {
                var expectedDate = DateTime.Today.AddDays(-(numberOfDaysAgoOfLastCleared - i));
                _localAuthorityRepositoryMock.Verify(r => r.ClearStagingDataForDateAsync(expectedDate, _cancellationToken),
                    Times.Once, $"Did not clear data for date {i} days ago");
            }
        }
    }
}