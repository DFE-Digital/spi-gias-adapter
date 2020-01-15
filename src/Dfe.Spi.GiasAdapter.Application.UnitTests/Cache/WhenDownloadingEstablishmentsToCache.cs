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
    public class WhenDownloadingEstablishmentsToCache
    {
        private Mock<IGiasApiClient> _giasApiClientMock;
        private Mock<IEstablishmentRepository> _establishmentRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEventPublisher> _eventPublisherMock;
        private Mock<IEstablishmentProcessingQueue> _establishmentProcessingQueueMock;
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
            
            _mapperMock = new Mock<IMapper>();
            
            _eventPublisherMock = new Mock<IEventPublisher>();
            
            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();
            
            _loggerMock = new Mock<ILoggerWrapper>();
            
            _manager = new CacheManager(
                _giasApiClientMock.Object,
                _establishmentRepositoryMock.Object,
                _mapperMock.Object,
                _eventPublisherMock.Object,
                _establishmentProcessingQueueMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldGetEstablishmentsFromGias()
        {
            await _manager.DownloadEstablishmentsToCacheAsync(_cancellationToken);
            
            _giasApiClientMock.Verify(c=>c.DownloadEstablishmentsAsync(_cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEstablishmentsInStaging(Establishment[] establishments)
        {
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(establishments);
            
            await _manager.DownloadEstablishmentsToCacheAsync(_cancellationToken);
            
            _establishmentRepositoryMock.Verify(r=>r.StoreInStagingAsync(establishments, _cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task ThenItShouldQueueBatchesOfUrnsForProcessing()
        {
            var establishments = new Establishment[1500];
            for (var i = 0; i < establishments.Length; i++)
            {
                establishments[i] = new Establishment
                {
                    Urn = 1000001 + i,
                };
            }
            _giasApiClientMock.Setup(c => c.DownloadEstablishmentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(establishments);
            
            await _manager.DownloadEstablishmentsToCacheAsync(_cancellationToken);

            var expectedBatch1 = establishments.Take(1000).Select(e => e.Urn).ToArray();
            var expectedBatch2 = establishments.Skip(1000).Take(1000).Select(e => e.Urn).ToArray();
            _establishmentProcessingQueueMock.Verify(q=>q.EnqueueBatchOfStagingAsync(
                It.Is<long[]>(urns => AreEqual(expectedBatch1, urns)), _cancellationToken),
                Times.Once);
            _establishmentProcessingQueueMock.Verify(q=>q.EnqueueBatchOfStagingAsync(
                It.Is<long[]>(urns => AreEqual(expectedBatch2, urns)), _cancellationToken),
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
    }
}