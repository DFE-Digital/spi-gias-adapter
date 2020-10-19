using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
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
    public class WhenDownloadingAllGiasDataToCache
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
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment[0]);

            _stateRepositoryMock = new Mock<IStateRepository>();
            
            _establishmentRepositoryMock = new Mock<IEstablishmentRepository>();

            _groupRepositoryMock = new Mock<IGroupRepository>();

            _localAuthorityRepositoryMock = new Mock<ILocalAuthorityRepository>();

            _mapperMock = new Mock<IMapper>();

            _eventPublisherMock = new Mock<IEventPublisher>();

            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();

            _groupProcessingQueueMock = new Mock<IGroupProcessingQueue>();

            _localAuthorityProcessingQueueMock = new Mock<ILocalAuthorityProcessingQueue>();

            _configuration = new CacheConfiguration();

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
        public async Task ThenItShouldGetEstablishmentsFromGias()
        {
            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            _giasApiClientMock.Verify(c => c.DownloadEstablishmentsAsync(_cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEstablishmentsInStaging(Establishment[] establishments)
        {
            foreach (var establishment in establishments)
            {
                establishment.LA.Code = "123";
            }

            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(establishments);

            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            _establishmentRepositoryMock.Verify(r => r.StoreInStagingAsync(
                    It.Is<PointInTimeEstablishment[]>(storedEstablishments => AreEqual(establishments, DateTime.UtcNow.Date, storedEstablishments)),
                    _cancellationToken),
                Times.Once);
        }


        [Test]
        public async Task ThenItShouldStoreUniqueLocalAuthorities()
        {
            var laCode1 = 101;
            var laName1 = "la one";
            var laCode2 = 202;
            var laName2 = "la two";
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Establishment {LA = new CodeNamePair {Code = laCode1.ToString(), DisplayName = laName1}},
                    new Establishment {LA = new CodeNamePair {Code = laCode2.ToString(), DisplayName = laName2}},
                    new Establishment {LA = new CodeNamePair {Code = laCode1.ToString(), DisplayName = laName1}},
                    new Establishment {LA = new CodeNamePair {Code = laCode1.ToString(), DisplayName = laName1}},
                    new Establishment {LA = new CodeNamePair {Code = laCode2.ToString(), DisplayName = laName2}},
                });

            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            _localAuthorityRepositoryMock.Verify(r => r.StoreInStagingAsync(
                    It.Is<PointInTimeLocalAuthority[]>(las => las.Length == 2),
                    _cancellationToken),
                Times.Once);
        }


        [Test]
        public async Task ThenItShouldGetGroupsFromGias()
        {
            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            _giasApiClientMock.Verify(c => c.DownloadGroupsAsync(_cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreGroupsInStaging(Group[] groups)
        {
            _giasApiClientMock.Setup(c => c.DownloadGroupsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(groups);

            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            _groupRepositoryMock.Verify(r => r.StoreInStagingAsync(
                    It.Is<PointInTimeGroup[]>(storedGroups => AreEqual(groups, DateTime.UtcNow.Date, storedGroups)),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldQueueGroupsWithChildEstablishments(
            long group1Uid, long group1Urn1, long group1Urn2,
            long group2Uid, long group2Urn1, long group2Urn2)
        {
            // Arrange
            _giasApiClientMock.Setup(c => c.DownloadGroupsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Group {Uid = group1Uid},
                    new Group {Uid = group2Uid},
                });
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Establishment {Urn = group1Urn1},
                    new Establishment {Urn = group1Urn2},
                    new Establishment {Urn = group2Urn1},
                    new Establishment {Urn = group2Urn2},
                });
            _giasApiClientMock.Setup(c => c.DownloadGroupLinksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new GroupLink {GroupType = "Federation", Uid = group1Uid, Urn = group1Urn1},
                    new GroupLink {GroupType = "Trust", Uid = group1Uid, Urn = group1Urn2},
                    new GroupLink {GroupType = "Single-academy trust", Uid = group2Uid, Urn = group2Urn1},
                    new GroupLink {GroupType = "Multi-academy trust", Uid = group2Uid, Urn = group2Urn2},
                });

            // Act
            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            // Assert
            _groupProcessingQueueMock.Verify(q => q.EnqueueStagingAsync(
                    It.IsAny<StagingBatchQueueItem<long>>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _groupProcessingQueueMock.Verify(q => q.EnqueueStagingAsync(
                    It.Is<StagingBatchQueueItem<long>>(item =>
                        item.ParentIdentifier == group1Uid &&
                        item.Urns.Length == 2 &&
                        item.Urns.Any(urn => urn == group1Urn1) &&
                        item.Urns.Any(urn => urn == group1Urn2)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _groupProcessingQueueMock.Verify(q => q.EnqueueStagingAsync(
                    It.Is<StagingBatchQueueItem<long>>(item =>
                        item.ParentIdentifier == group2Uid &&
                        item.Urns.Length == 2 &&
                        item.Urns.Any(urn => urn == group2Urn1) &&
                        item.Urns.Any(urn => urn == group2Urn2)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldQueueLocalAuthoritiesWithChildEstablishments(
            int la1Code, long la1Urn1, long la1Urn2,
            int la2Code, long la2Urn1, long la2Urn2)
        {
            // Arrange
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[]
                {
                    new Establishment {Urn = la1Urn1, LA = new CodeNamePair { Code = la1Code.ToString()}},
                    new Establishment {Urn = la1Urn2, LA = new CodeNamePair { Code = la1Code.ToString()}},
                    new Establishment {Urn = la2Urn1, LA = new CodeNamePair { Code = la2Code.ToString()}},
                    new Establishment {Urn = la2Urn2, LA = new CodeNamePair { Code = la2Code.ToString()}},
                });

            // Act
            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            // Assert
            _localAuthorityProcessingQueueMock.Verify(q => q.EnqueueStagingAsync(
                    It.IsAny<StagingBatchQueueItem<int>>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _localAuthorityProcessingQueueMock.Verify(q => q.EnqueueStagingAsync(
                    It.Is<StagingBatchQueueItem<int>>(item =>
                        item.ParentIdentifier == la1Code &&
                        item.Urns.Length == 2 &&
                        item.Urns.Any(urn => urn == la1Urn1) &&
                        item.Urns.Any(urn => urn == la1Urn2)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _localAuthorityProcessingQueueMock.Verify(q => q.EnqueueStagingAsync(
                    It.Is<StagingBatchQueueItem<int>>(item =>
                        item.ParentIdentifier == la2Code &&
                        item.Urns.Length == 2 &&
                        item.Urns.Any(urn => urn == la2Urn1) &&
                        item.Urns.Any(urn => urn == la2Urn2)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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

        private bool AreEqual(int[] expected, int[] actual)
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

        private bool AreEqual(Establishment[] expectedEstablishments, DateTime expectedPointInTime, PointInTimeEstablishment[] actual)
        {
            if (expectedEstablishments.Length != actual.Length)
            {
                return false;
            }

            foreach (var expectedEstablishment in expectedEstablishments)
            {
                var actualGroup = actual.SingleOrDefault(x => x.Urn == expectedEstablishment.Urn);
                if (actualGroup == null)
                {
                    return false;
                }

                if (actualGroup.PointInTime != expectedPointInTime)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AreEqual(Group[] expectedGroups, DateTime expectedPointInTime, PointInTimeGroup[] actual)
        {
            if (expectedGroups.Length != actual.Length)
            {
                return false;
            }

            foreach (var expectedGroup in expectedGroups)
            {
                var actualGroup = actual.SingleOrDefault(x => x.Uid == expectedGroup.Uid);
                if (actualGroup == null)
                {
                    return false;
                }

                if (actualGroup.PointInTime != expectedPointInTime)
                {
                    return false;
                }
            }

            return true;
        }
    }
}