using System.Threading;
using System.Threading.Tasks;
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
    public class WhenProcessingBatchOfLocalAuthorities
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
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocalAuthority) null);
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityFromStagingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int laCode, CancellationToken cancellationToken) => new LocalAuthority
                {
                    Code = laCode,
                    Name = laCode.ToString()
                });

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m=>m.MapAsync<ManagementGroup>(It.IsAny<LocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocalAuthority localAuthority, CancellationToken cancellationToken) => new ManagementGroup
                {
                    Name = localAuthority.Name,
                });

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
        public async Task ThenItShouldProcessEveryLaCode()
        {
            var laCode = new[] {101, 202};

            await _manager.ProcessBatchOfLocalAuthorities(laCode, _cancellationToken);

            _localAuthorityRepositoryMock.Verify(
                r => r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _localAuthorityRepositoryMock.Verify(r => r.GetLocalAuthorityAsync(laCode[0], _cancellationToken),
                Times.Once);
            _localAuthorityRepositoryMock.Verify(r => r.GetLocalAuthorityAsync(laCode[1], _cancellationToken),
                Times.Once);

            _localAuthorityRepositoryMock.Verify(
                r => r.GetLocalAuthorityFromStagingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _localAuthorityRepositoryMock.Verify(r => r.GetLocalAuthorityFromStagingAsync(laCode[0], _cancellationToken),
                Times.Once);
            _localAuthorityRepositoryMock.Verify(r => r.GetLocalAuthorityFromStagingAsync(laCode[1], _cancellationToken),
                Times.Once);

            _localAuthorityRepositoryMock.Verify(
                r => r.StoreAsync(It.IsAny<LocalAuthority>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _localAuthorityRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<LocalAuthority>(e => e.Code == laCode[0]), _cancellationToken),
                Times.Once);
            _localAuthorityRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<LocalAuthority>(e => e.Code == laCode[1]), _cancellationToken),
                Times.Once);

            _mapperMock.Verify(
                m => m.MapAsync<ManagementGroup>(It.IsAny<LocalAuthority>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _mapperMock.Verify(
                m => m.MapAsync<ManagementGroup>(It.Is<LocalAuthority>(e => e.Code == laCode[0]), It.IsAny<CancellationToken>()),
                Times.Once);
            _mapperMock.Verify(
                m => m.MapAsync<ManagementGroup>(It.Is<LocalAuthority>(e => e.Code == laCode[1]), It.IsAny<CancellationToken>()),
                Times.Once);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupCreatedAsync(It.IsAny<ManagementGroup>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishCreatedEventIfNoCurrent(int laCode, ManagementGroup managementGroup)
        {
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LocalAuthority) null);
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityFromStagingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority
                {
                    Code = laCode,
                    Name = laCode.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<ManagementGroup>(It.IsAny<LocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            
            await _manager.ProcessBatchOfLocalAuthorities(new[]{laCode}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupCreatedAsync(managementGroup, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishUpdatedEventIfCurrentThatHasChanged(int laCode, ManagementGroup managementGroup)
        {
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority
                {
                    Code = laCode,
                    Name = "old name"
                });
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityFromStagingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority
                {
                    Code = laCode,
                    Name = laCode.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<ManagementGroup>(It.IsAny<LocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            
            await _manager.ProcessBatchOfLocalAuthorities(new[]{laCode}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupUpdatedAsync(managementGroup, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldNotPublishAnyEventIfCurrentThatHasNotChanged(int laCode)
        {
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority
                {
                    Code = laCode,
                    Name = laCode.ToString()
                });
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityFromStagingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority
                {
                    Code = laCode,
                    Name = laCode.ToString()
                }); 
            
            await _manager.ProcessBatchOfLocalAuthorities(new[]{laCode}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupCreatedAsync(It.IsAny<ManagementGroup>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupUpdatedAsync(It.IsAny<ManagementGroup>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}