using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
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
    public class WhenProcessingGroup
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
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<PointInTimeGroup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PointInTimeGroup group, CancellationToken ct) =>
                    new ManagementGroup
                    {
                        Identifier = group.Uid.ToString(),
                    });
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PointInTimeEstablishment establishment, CancellationToken ct) =>
                    new LearningProvider
                    {
                        Urn = establishment.Urn,
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

        [Test, AutoData]
        public async Task ThenItShouldStoreTheGroupIfDoesNotAlreadyExist(PointInTimeGroup stagingGroup)
        {
            // Arrange
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {1000001L}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _groupRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeGroup[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingGroup),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreTheGroupIfAlreadyExistsAndUpdated(PointInTimeGroup stagingGroup)
        {
            // Arrange
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(CloneWithChanges(stagingGroup, stagingGroup.GroupName + "-updated", false));
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {1000001L}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _groupRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeGroup[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingGroup),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForGroupIfDoesNotAlreadyExist(PointInTimeGroup stagingGroup)
        {
            // Arrange
            var managementGroup = new ManagementGroup {Identifier = stagingGroup.Uid.ToString()};
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<PointInTimeGroup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {1000001L}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishManagementGroupCreatedAsync(
                    managementGroup,
                    stagingGroup.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForGroupIfAlreadyExistsAndUpdated(PointInTimeGroup stagingGroup)
        {
            // Arrange
            var managementGroup = new ManagementGroup {Identifier = stagingGroup.Uid.ToString()};
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<PointInTimeGroup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(CloneWithChanges(stagingGroup, stagingGroup.GroupName + "-updated", false));
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {1000001L}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishManagementGroupUpdatedAsync(
                    managementGroup,
                    stagingGroup.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEstablishmentIfDoesNotAlreadyExist(PointInTimeGroup stagingGroup, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {stagingEstablishment.Urn}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _establishmentRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeEstablishment[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingEstablishment),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEstablishmentIfAlreadyExistsAndUpdated(PointInTimeGroup stagingGroup, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CloneWithChanges(stagingEstablishment, stagingEstablishment.EstablishmentName + "-updated", false));

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {stagingEstablishment.Urn}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _establishmentRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeEstablishment[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingEstablishment),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForEstablishmentIfDoesNotAlreadyExist(PointInTimeGroup stagingGroup, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            var learningProvider = new LearningProvider {Urn = stagingEstablishment.Urn};
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {stagingEstablishment.Urn}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishLearningProviderCreatedAsync(
                    learningProvider,
                    stagingEstablishment.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForEstablishmentIfAlreadyExistsAndUpdated(PointInTimeGroup stagingGroup, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            var learningProvider = new LearningProvider {Urn = stagingEstablishment.Urn};
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CloneWithChanges(stagingEstablishment, stagingEstablishment.EstablishmentName + "-updated", false));

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {stagingEstablishment.Urn}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishLearningProviderUpdatedAsync(
                    learningProvider,
                    stagingEstablishment.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForEstablishmentIfAlreadyExistsNotUpdatedButGroupIsUpdated(PointInTimeGroup stagingGroup, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            var learningProvider = new LearningProvider {Urn = stagingEstablishment.Urn};
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            _groupRepositoryMock.Setup(r => r.GetGroupFromStagingAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingGroup);
            _groupRepositoryMock.Setup(r => r.GetGroupAsync(stagingGroup.Uid, stagingGroup.PointInTime, _cancellationToken))
                .ReturnsAsync(CloneWithChanges(stagingGroup, stagingGroup.GroupName + "-updated", false));
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Clone(stagingEstablishment));

            // Act
            await _manager.ProcessGroupAsync(stagingGroup.Uid, new[] {stagingEstablishment.Urn}, stagingGroup.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishLearningProviderUpdatedAsync(
                    learningProvider,
                    stagingEstablishment.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        private PointInTimeGroup CloneWithChanges(PointInTimeGroup group, string groupName = null, bool? isCurrent = null)
        {
            var clone = Clone(group);

            if (groupName != null)
            {
                clone.GroupName = groupName;
            }

            if (isCurrent.HasValue)
            {
                clone.IsCurrent = isCurrent.Value;
            }

            return clone;
        }

        private PointInTimeEstablishment CloneWithChanges(PointInTimeEstablishment group, string establishmentName = null, bool? isCurrent = null)
        {
            var clone = Clone(group);

            if (establishmentName != null)
            {
                clone.EstablishmentName = establishmentName;
            }

            if (isCurrent.HasValue)
            {
                clone.IsCurrent = isCurrent.Value;
            }

            return clone;
        }

        private T Clone<T>(T source)
            where T : class, new()
        {
            if (source == null)
            {
                return null;
            }

            var properties = source.GetType().GetProperties();
            var clone = new T();

            foreach (var property in properties)
            {
                var sourceValue = property.GetValue(source);
                property.SetValue(clone, sourceValue);
            }

            return clone;
        }
    }
}