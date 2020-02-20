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
    public class WhenProcessingBatchOfGroups
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
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group) null);
            _groupRepositoryMock.Setup(r =>
                    r.GetGroupFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long uid, CancellationToken cancellationToken) => new Group
                {
                    Uid = uid,
                    GroupName = uid.ToString()
                });

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m=>m.MapAsync<ManagementGroup>(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group group, CancellationToken cancellationToken) => new ManagementGroup
                {
                    Name = group.GroupName,
                });

            _eventPublisherMock = new Mock<IEventPublisher>();

            _establishmentProcessingQueueMock = new Mock<IEstablishmentProcessingQueue>();
            
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
        public async Task ThenItShouldProcessEveryUid()
        {
            var uids = new[] {100001L, 100002L};

            await _manager.ProcessBatchOfGroups(uids, _cancellationToken);

            _groupRepositoryMock.Verify(
                r => r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _groupRepositoryMock.Verify(r => r.GetGroupAsync(uids[0], _cancellationToken),
                Times.Once);
            _groupRepositoryMock.Verify(r => r.GetGroupAsync(uids[1], _cancellationToken),
                Times.Once);

            _groupRepositoryMock.Verify(
                r => r.GetGroupFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _groupRepositoryMock.Verify(r => r.GetGroupFromStagingAsync(uids[0], _cancellationToken),
                Times.Once);
            _groupRepositoryMock.Verify(r => r.GetGroupFromStagingAsync(uids[1], _cancellationToken),
                Times.Once);

            _groupRepositoryMock.Verify(
                r => r.StoreAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _groupRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<Group>(e => e.Uid == uids[0]), _cancellationToken),
                Times.Once);
            _groupRepositoryMock.Verify(
                r => r.StoreAsync(It.Is<Group>(e => e.Uid == uids[1]), _cancellationToken),
                Times.Once);

            _mapperMock.Verify(
                m => m.MapAsync<ManagementGroup>(It.IsAny<Group>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            _mapperMock.Verify(
                m => m.MapAsync<ManagementGroup>(It.Is<Group>(e => e.Uid == uids[0]), It.IsAny<CancellationToken>()),
                Times.Once);
            _mapperMock.Verify(
                m => m.MapAsync<ManagementGroup>(It.Is<Group>(e => e.Uid == uids[1]), It.IsAny<CancellationToken>()),
                Times.Once);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupCreatedAsync(It.IsAny<ManagementGroup>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishCreatedEventIfNoCurrent(long uid, ManagementGroup managementGroup)
        {
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group) null);
            _groupRepositoryMock.Setup(r =>
                    r.GetGroupFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Group
                {
                    Uid = uid,
                    GroupName = uid.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<ManagementGroup>(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            
            await _manager.ProcessBatchOfGroups(new[]{uid}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupCreatedAsync(managementGroup, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldPublishUpdatedEventIfCurrentThatHasChanged(long uid, ManagementGroup managementGroup)
        {
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Group
                {
                    Uid = uid,
                    GroupName = "old name"
                });
            _groupRepositoryMock.Setup(r =>
                    r.GetGroupFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Group
                {
                    Uid = uid,
                    GroupName = uid.ToString()
                }); 
            _mapperMock.Setup(m=>m.MapAsync<ManagementGroup>(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            
            await _manager.ProcessBatchOfGroups(new[]{uid}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupUpdatedAsync(managementGroup, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldNotPublishAnyEventIfCurrentThatHasNotChanged(long uid)
        {
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Group
                {
                    Uid = uid,
                    GroupName = uid.ToString()
                });
            _groupRepositoryMock.Setup(r =>
                    r.GetGroupFromStagingAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Group
                {
                    Uid = uid,
                    GroupName = uid.ToString()
                }); 
            
            await _manager.ProcessBatchOfGroups(new[]{uid}, _cancellationToken);

            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupCreatedAsync(It.IsAny<ManagementGroup>(), It.IsAny<CancellationToken>()),
                Times.Never);
            _eventPublisherMock.Verify(
                p => p.PublishManagementGroupUpdatedAsync(It.IsAny<ManagementGroup>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}