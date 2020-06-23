using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Domain.Cache;
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
        private Mock<IEstablishmentRepository> _establishmentRepositoryMock;
        private Mock<IGroupRepository> _groupRepositoryMock;
        private Mock<ILocalAuthorityRepository> _localAuthorityRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEventPublisher> _eventPublisherMock;
        private Mock<IEstablishmentProcessingQueue> _establishmentProcessingQueueMock;
        private Mock<IGroupProcessingQueue> _groupProcessingQueueMock;
        private Mock<ILocalAuthorityProcessingQueue> _localAuthorityProcessingQueueMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private CacheManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _giasApiClientMock = new Mock<IGiasApiClient>();
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment[0]);

            _establishmentRepositoryMock = new Mock<IEstablishmentRepository>();

            _groupRepositoryMock = new Mock<IGroupRepository>();

            _localAuthorityRepositoryMock = new Mock<ILocalAuthorityRepository>();

            _mapperMock = new Mock<IMapper>();

            _eventPublisherMock = new Mock<IEventPublisher>();

            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();

            _groupProcessingQueueMock = new Mock<IGroupProcessingQueue>();
            
            _localAuthorityProcessingQueueMock = new Mock<ILocalAuthorityProcessingQueue>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new CacheManager(
                _giasApiClientMock.Object,
                _establishmentRepositoryMock.Object,
                _groupRepositoryMock.Object,
                _localAuthorityRepositoryMock.Object,
                _mapperMock.Object,
                _eventPublisherMock.Object,
                _establishmentProcessingQueueMock.Object,
                _groupProcessingQueueMock.Object,
                _localAuthorityProcessingQueueMock.Object,
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
        public async Task ThenItShouldQueueBatchesOfUrnsForProcessing()
        {
            var establishments = new Establishment[150];
            for (var i = 0; i < establishments.Length; i++)
            {
                establishments[i] = new Establishment
                {
                    Urn = 1000001 + i,
                };
            }

            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(establishments);

            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            var expectedBatch1 = establishments.Take(100).Select(e => e.Urn).ToArray();
            var expectedBatch2 = establishments.Skip(100).Take(100).Select(e => e.Urn).ToArray();
            _establishmentProcessingQueueMock.Verify(q => q.EnqueueBatchOfStagingAsync(
                    It.Is<long[]>(urns => AreEqual(expectedBatch1, urns)), _cancellationToken),
                Times.Once);
            _establishmentProcessingQueueMock.Verify(q => q.EnqueueBatchOfStagingAsync(
                    It.Is<long[]>(urns => AreEqual(expectedBatch2, urns)), _cancellationToken),
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
        public async Task ThenItShouldQueueBatchesOfLaCodesForProcessing()
        {
            var establishments = new Establishment[150];
            for (var i = 0; i < establishments.Length; i++)
            {
                establishments[i] = new Establishment
                {
                    LA = new CodeNamePair
                    {
                        Code = (1000001 + i).ToString()
                    }
                };
            }
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(establishments);

            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            var expectedBatch1 = establishments.Take(100).Select(e => int.Parse(e.LA.Code)).ToArray();
            var expectedBatch2 = establishments.Skip(100).Take(100).Select(e => int.Parse(e.LA.Code)).ToArray();
            _localAuthorityProcessingQueueMock.Verify(q => q.EnqueueBatchOfStagingAsync(
                    It.Is<int[]>(laCodes => AreEqual(expectedBatch1, laCodes)), _cancellationToken),
                Times.Once);
            _localAuthorityProcessingQueueMock.Verify(q => q.EnqueueBatchOfStagingAsync(
                    It.Is<int[]>(laCodes => AreEqual(expectedBatch2, laCodes)), _cancellationToken),
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

        [Test]
        public async Task ThenItShouldQueueBatchesOfUidsForProcessing()
        {
            var groups = new Group[150];
            for (var i = 0; i < groups.Length; i++)
            {
                groups[i] = new Group
                {
                    Uid = 1000001 + i,
                };
            }

            _giasApiClientMock.Setup(c => c.DownloadGroupsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(groups);

            await _manager.DownloadAllGiasDataToCacheAsync(_cancellationToken);

            var expectedBatch1 = groups.Take(100).Select(e => e.Uid).ToArray();
            var expectedBatch2 = groups.Skip(100).Take(100).Select(e => e.Uid).ToArray();
            _groupProcessingQueueMock.Verify(q => q.EnqueueBatchOfStagingAsync(
                    It.Is<long[]>(uids => AreEqual(expectedBatch1, uids)), _cancellationToken),
                Times.Once);
            _groupProcessingQueueMock.Verify(q => q.EnqueueBatchOfStagingAsync(
                    It.Is<long[]>(uids => AreEqual(expectedBatch2, uids)), _cancellationToken),
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