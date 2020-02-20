using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.GiasAdapter.Application.Cache;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Events;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Application.UnitTests.Cache
{
    public class WhenProcessingBatchOfEstablishments
    {
        private Mock<IGiasApiClient> _giasApiClientMock;
        private Mock<IEstablishmentRepository> _establishmentRepositoryMock;
        private Mock<IGroupRepository> _groupRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IEventPublisher> _eventPublisherMock;
        private Mock<IEstablishmentProcessingQueue> _establishmentProcessingQueueMock;
        private Mock<IGroupProcessingQueue> _groupProcessingQueueMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private CacheManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _giasApiClientMock = new Mock<IGiasApiClient>();

            _establishmentRepositoryMock = new Mock<IEstablishmentRepository>();
            
            _groupRepositoryMock = new Mock<IGroupRepository>();

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m=>m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Establishment establishment, CancellationToken cancellationToken) => new LearningProvider
                {
                    Name = establishment.EstablishmentName,
                });

            _eventPublisherMock = new Mock<IEventPublisher>();

            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Establishment) null);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long urn, CancellationToken cancellationToken) => new Establishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                });
            
            _groupProcessingQueueMock = new Mock<IGroupProcessingQueue>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new CacheManager(
                _giasApiClientMock.Object,
                _establishmentRepositoryMock.Object,
                _groupRepositoryMock.Object,
                _mapperMock.Object,
                _eventPublisherMock.Object,
                _establishmentProcessingQueueMock.Object,
                _groupProcessingQueueMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test]
        public async Task ThenItShouldProcessEveryUrn()
        {
            var urns = new[] {100001L, 100002L};

            await _manager.ProcessBatchOfEstablishments(urns, _cancellationToken);

            _establishmentRepositoryMock.Verify(
                r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentAsync(urns[0], _cancellationToken),
                Times.Once);
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentAsync(urns[1], _cancellationToken),
                Times.Once);

            _establishmentRepositoryMock.Verify(
                r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentFromStagingAsync(urns[0], _cancellationToken),
                Times.Once);
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentFromStagingAsync(urns[1], _cancellationToken),
                Times.Once);

            _establishmentRepositoryMock.Verify(
                r => r.StoreAsync(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _establishmentRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<Establishment>(e => e.Urn == urns[0]), _cancellationToken),
                Times.Once);
            _establishmentRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<Establishment>(e => e.Urn == urns[1]), _cancellationToken),
                Times.Once);

            _mapperMock.Verify(
                m => m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _mapperMock.Verify(
                m => m.MapAsync<LearningProvider>(It.Is<Establishment>(e => e.Urn == urns[0]), It.IsAny<CancellationToken>()),
                Times.Once);
            _mapperMock.Verify(
                m => m.MapAsync<LearningProvider>(It.Is<Establishment>(e => e.Urn == urns[1]), It.IsAny<CancellationToken>()),
                Times.Once);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderCreatedAsync(It.IsAny<LearningProvider>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishCreatedEventIfNoCurrent(long urn, LearningProvider learningProvider)
        {
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Establishment) null);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            
            await _manager.ProcessBatchOfEstablishments(new[]{urn}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderCreatedAsync(learningProvider, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishUpdatedEventIfCurrentThatHasChanged(long urn, LearningProvider learningProvider)
        {
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment
                {
                    Urn = urn,
                    EstablishmentName = "old name"
                });
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            
            await _manager.ProcessBatchOfEstablishments(new[]{urn}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderUpdatedAsync(learningProvider, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldNotPublishAnyEventIfCurrentThatHasNotChanged(long urn)
        {
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                });
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Establishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                }); 
            
            await _manager.ProcessBatchOfEstablishments(new[]{urn}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderCreatedAsync(It.IsAny<LearningProvider>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderUpdatedAsync(It.IsAny<LearningProvider>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}