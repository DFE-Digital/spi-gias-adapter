using System;
using System.Linq;
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

            _establishmentRepositoryMock = new Mock<IEstablishmentRepository>();
            
            _groupRepositoryMock = new Mock<IGroupRepository>();
            
            _localAuthorityRepositoryMock = new Mock<ILocalAuthorityRepository>();

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m=>m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Establishment establishment, CancellationToken cancellationToken) => new LearningProvider
                {
                    Name = establishment.EstablishmentName,
                });

            _eventPublisherMock = new Mock<IEventPublisher>();

            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PointInTimeEstablishment) null);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long urn, DateTime pointInTime, CancellationToken cancellationToken) => new PointInTimeEstablishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString(),
                    PointInTime = pointInTime,
                });
            
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
        public async Task ThenItShouldProcessEveryUrn()
        {
            var urns = new[] {100001L, 100002L};
            var pointInTime = DateTime.Now.Date;

            await _manager.ProcessBatchOfEstablishments(urns, pointInTime, _cancellationToken);

            _establishmentRepositoryMock.Verify(
                r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentAsync(urns[0], _cancellationToken),
                Times.Once);
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentAsync(urns[1], _cancellationToken),
                Times.Once);

            _establishmentRepositoryMock.Verify(
                r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentFromStagingAsync(urns[0], pointInTime, _cancellationToken),
                Times.Once);
            _establishmentRepositoryMock.Verify(r => r.GetEstablishmentFromStagingAsync(urns[1], pointInTime, _cancellationToken),
                Times.Once);

            _establishmentRepositoryMock.Verify(
                r => r.StoreAsync(It.IsAny<PointInTimeEstablishment[]>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _establishmentRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<PointInTimeEstablishment[]>(e => e.First().Urn == urns[0]), _cancellationToken),
                Times.Once);
            _establishmentRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<PointInTimeEstablishment[]>(e => e.First().Urn == urns[1]), _cancellationToken),
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
        public async Task ThenItShouldPublishCreatedEventIfNoCurrent(long urn, DateTime pointInTime, LearningProvider learningProvider)
        {
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PointInTimeEstablishment) null);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            
            await _manager.ProcessBatchOfEstablishments(new[]{urn}, pointInTime, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderCreatedAsync(learningProvider, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishUpdatedEventIfCurrentThatHasChanged(long urn, DateTime pointInTime, LearningProvider learningProvider)
        {
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment
                {
                    Urn = urn,
                    EstablishmentName = "old name"
                });
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            
            await _manager.ProcessBatchOfEstablishments(new[]{urn}, pointInTime, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderUpdatedAsync(learningProvider, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldNotPublishAnyEventIfCurrentThatHasNotChanged(long urn, DateTime pointInTime)
        {
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                });
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment
                {
                    Urn = urn,
                    EstablishmentName = urn.ToString()
                }); 
            
            await _manager.ProcessBatchOfEstablishments(new[]{urn}, pointInTime, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderCreatedAsync(It.IsAny<LearningProvider>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _eventPublisherMock.Verify(
                p => p.PublishLearningProviderUpdatedAsync(It.IsAny<LearningProvider>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}