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
    public class WhenProcessingLocalAuthority
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
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<PointInTimeLocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PointInTimeLocalAuthority localAuthority, CancellationToken ct) =>
                    new ManagementGroup
                    {
                        Identifier = localAuthority.Code.ToString(),
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
        public async Task ThenItShouldStoreTheGroupIfDoesNotAlreadyExist(PointInTimeLocalAuthority stagingLocalAuthority)
        {
            // Arrange
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {1000001L}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _localAuthorityRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeLocalAuthority[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingLocalAuthority),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreTheGroupIfAlreadyExistsAndUpdated(PointInTimeLocalAuthority stagingLocalAuthority)
        {
            // Arrange
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(CloneWithChanges(stagingLocalAuthority, stagingLocalAuthority.Name + "-updated", false));
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {1000001L}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _localAuthorityRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeLocalAuthority[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingLocalAuthority),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForGroupIfDoesNotAlreadyExist(PointInTimeLocalAuthority stagingLocalAuthority)
        {
            // Arrange
            var managementGroup = new ManagementGroup {Identifier = stagingLocalAuthority.Code.ToString()};
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<PointInTimeLocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {1000001L}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishManagementGroupCreatedAsync(
                    managementGroup,
                    stagingLocalAuthority.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForGroupIfAlreadyExistsAndUpdated(PointInTimeLocalAuthority stagingLocalAuthority)
        {
            // Arrange
            var managementGroup = new ManagementGroup {Identifier = stagingLocalAuthority.Code.ToString()};
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<PointInTimeLocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(CloneWithChanges(stagingLocalAuthority, stagingLocalAuthority.Name + "-updated", false));
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentFromStagingAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PointInTimeEstablishment());

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {1000001L}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishManagementGroupUpdatedAsync(
                    managementGroup,
                    stagingLocalAuthority.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEstablishmentIfDoesNotAlreadyExist(PointInTimeLocalAuthority stagingLocalAuthority, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {stagingEstablishment.Urn}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _establishmentRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeEstablishment[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingEstablishment),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldStoreEstablishmentIfAlreadyExistsAndUpdated(PointInTimeLocalAuthority stagingLocalAuthority, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CloneWithChanges(stagingEstablishment, stagingEstablishment.EstablishmentName + "-updated", false));

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {stagingEstablishment.Urn}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _establishmentRepositoryMock.Verify(r => r.StoreAsync(
                    It.Is<PointInTimeEstablishment[]>(toStore =>
                        toStore.Length == 1 &&
                        toStore[0] == stagingEstablishment),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForEstablishmentIfDoesNotAlreadyExist(PointInTimeLocalAuthority stagingLocalAuthority, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            var learningProvider = new LearningProvider {Urn = stagingEstablishment.Urn};
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {stagingEstablishment.Urn}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishLearningProviderCreatedAsync(
                    learningProvider,
                    stagingEstablishment.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForEstablishmentIfAlreadyExistsAndUpdated(PointInTimeLocalAuthority stagingLocalAuthority, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            var learningProvider = new LearningProvider {Urn = stagingEstablishment.Urn};
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(CloneWithChanges(stagingEstablishment, stagingEstablishment.EstablishmentName + "-updated", false));

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {stagingEstablishment.Urn}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishLearningProviderUpdatedAsync(
                    learningProvider,
                    stagingEstablishment.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldRaiseEventForEstablishmentIfAlreadyExistsNotUpdatedButGroupIsUpdated(PointInTimeLocalAuthority stagingLocalAuthority, PointInTimeEstablishment stagingEstablishment)
        {
            // Arrange
            var learningProvider = new LearningProvider {Urn = stagingEstablishment.Urn};
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<PointInTimeEstablishment>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(learningProvider);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityFromStagingAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(stagingLocalAuthority);
            _localAuthorityRepositoryMock.Setup(r => r.GetLocalAuthorityAsync(stagingLocalAuthority.Code, stagingLocalAuthority.PointInTime, _cancellationToken))
                .ReturnsAsync(CloneWithChanges(stagingLocalAuthority, stagingLocalAuthority.Name + "-updated", false));
            _establishmentRepositoryMock.Setup(r =>
                    r.GetEstablishmentFromStagingAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(stagingEstablishment);
            _establishmentRepositoryMock.Setup(r => r.GetEstablishmentAsync(stagingEstablishment.Urn, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Clone(stagingEstablishment));

            // Act
            await _manager.ProcessLocalAuthorityAsync(stagingLocalAuthority.Code, new[] {stagingEstablishment.Urn}, stagingLocalAuthority.PointInTime, _cancellationToken);

            // Assert
            _eventPublisherMock.Verify(p => p.PublishLearningProviderUpdatedAsync(
                    learningProvider,
                    stagingEstablishment.PointInTime,
                    _cancellationToken),
                Times.Once);
        }

        private PointInTimeLocalAuthority CloneWithChanges(PointInTimeLocalAuthority localAuthority, string name = null, bool? isCurrent = null)
        {
            var clone = Clone(localAuthority);

            if (name != null)
            {
                clone.Name = name;
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