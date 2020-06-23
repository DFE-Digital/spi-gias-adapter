using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.Models.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.UnitTests.PocoMapping
{
    public class WhenMappingEstablishmentToLearningProvider
    {
        private const string LocalAuthorityManagementGroupType = "unit-test-la";

        private Mock<ITranslator> _translatorMock;
        private Mock<IGroupRepository> _groupRepositoryMock;
        private Mock<ILocalAuthorityRepository> _localAuthorityRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private EstablishmentMapper _mapper;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _translatorMock = new Mock<ITranslator>();
            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ManagementGroupType, LocalAuthority.ManagementGroupType,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(LocalAuthorityManagementGroupType);

            _groupRepositoryMock = new Mock<IGroupRepository>();

            _localAuthorityRepositoryMock = new Mock<ILocalAuthorityRepository>();
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority());

            _mapperMock = new Mock<IMapper>();
            _mapperMock.Setup(m => m.MapAsync<ManagementGroup>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ManagementGroup());

            _mapper = new EstablishmentMapper(
                _translatorMock.Object,
                _groupRepositoryMock.Object,
                _localAuthorityRepositoryMock.Object,
                _mapperMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProvider(Establishment source)
        {
            EnsureManagmentGroupCodes(source);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<LearningProvider>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapEstablishmentToLearningProviderForBasicTypeProperties(Establishment source)
        {
            EnsureManagmentGroupCodes(source, trustUid: 987);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);
            string expectedDfeNumber = EstablishmentMapper.CreateDfeNumber(source);

            Assert.IsNotNull(actual);
            Assert.AreEqual(source.EstablishmentName, actual.Name);
            Assert.AreEqual(source.Urn, actual.Urn);
            Assert.AreEqual(source.Ukprn, actual.Ukprn);
            Assert.AreEqual(source.Ukprn, actual.Ukprn);
            Assert.AreEqual(source.Uprn, actual.Uprn);
            Assert.AreEqual(source.CompaniesHouseNumber, actual.CompaniesHouseNumber);
            Assert.AreEqual(source.CharitiesCommissionNumber, actual.CharitiesCommissionNumber);
            Assert.AreEqual(source.Trusts.Code?.ToString(), actual.AcademyTrustCode);
            Assert.AreEqual(expectedDfeNumber, actual.DfeNumber);
            Assert.AreEqual(source.EstablishmentNumber, actual.EstablishmentNumber);
            Assert.AreEqual(source.PreviousEstablishmentNumber, actual.PreviousEstablishmentNumber);
            Assert.AreEqual(source.Postcode, actual.Postcode);
            Assert.AreEqual(source.OpenDate, actual.OpenDate);
            Assert.AreEqual(source.CloseDate, actual.CloseDate);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapStatusFromTranslation(Establishment source, string transformedValue)
        {
            EnsureManagmentGroupCodes(source);

            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ProviderStatus, It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(transformedValue);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual(transformedValue, actual.Status);
            _translatorMock.Verify(
                t => t.TranslateEnumValue(EnumerationNames.ProviderStatus, source.EstablishmentStatus.Code.ToString(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapTypeFromTranslation(Establishment source, string transformedValue)
        {
            EnsureManagmentGroupCodes(source);

            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ProviderType, It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(transformedValue);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual(transformedValue, actual.Type);
            _translatorMock.Verify(
                t => t.TranslateEnumValue(EnumerationNames.ProviderType, source.EstablishmentTypeGroup.Code.ToString(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapSubTypeFromTranslation(Establishment source, string transformedValue)
        {
            EnsureManagmentGroupCodes(source);

            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ProviderSubType, It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(transformedValue);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual(transformedValue, actual.SubType);
            _translatorMock.Verify(
                t => t.TranslateEnumValue(EnumerationNames.ProviderSubType, source.TypeOfEstablishment.Code.ToString(),
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapManagementGroupToLAIfNoLinks(Establishment source, int laCode)
        {
            EnsureManagmentGroupCodes(source, laCode: laCode);
            var localAuthority = new LocalAuthority();
            var managementGroup = new ManagementGroup();
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(localAuthority);
            _mapperMock.Setup(m =>
                    m.MapAsync<ManagementGroup>(It.IsAny<LocalAuthority>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual.ManagementGroup);
            Assert.AreSame(managementGroup, actual.ManagementGroup);
            _localAuthorityRepositoryMock.Verify(r => r.GetLocalAuthorityAsync(laCode, _cancellationToken),
                Times.Once);
            _mapperMock.Verify(m => m.MapAsync<ManagementGroup>(localAuthority, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapManagementGroupToTrustIfAvailable(Establishment source, long uid)
        {
            EnsureManagmentGroupCodes(source, trustUid: uid);
            var trust = new PointInTimeGroup();
            var managementGroup = new ManagementGroup();
            _groupRepositoryMock.Setup(r =>
                    r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(trust);
            _mapperMock.Setup(m =>
                    m.MapAsync<ManagementGroup>(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual.ManagementGroup);
            Assert.AreSame(managementGroup, actual.ManagementGroup);
            _groupRepositoryMock.Verify(r=>r.GetGroupAsync(uid, _cancellationToken),
                Times.Once);
            _mapperMock.Verify(m => m.MapAsync<ManagementGroup>(trust, _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapManagementGroupToFederationIfAvailable(Establishment source, long uid)
        {
            EnsureManagmentGroupCodes(source, federationUid: uid);
            var federation = new PointInTimeGroup();
            var managementGroup = new ManagementGroup();
            _groupRepositoryMock.Setup(r =>
                    r.GetGroupAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(federation);
            _mapperMock.Setup(m =>
                    m.MapAsync<ManagementGroup>(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);

            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual.ManagementGroup);
            Assert.AreSame(managementGroup, actual.ManagementGroup);
            _groupRepositoryMock.Verify(r=>r.GetGroupAsync(uid, _cancellationToken),
                Times.Once);
            _mapperMock.Verify(m => m.MapAsync<ManagementGroup>(federation, _cancellationToken),
                Times.Once);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfSourceIsNotEstablishment()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _mapper.MapAsync<LearningProvider>(new object(), new CancellationToken()));
        }

        [Test]
        public void ThenItShouldThrowExceptionIfDestinationIsNotLearningProvider()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _mapper.MapAsync<object>(new Establishment(), new CancellationToken()));
        }


        private void EnsureManagmentGroupCodes(Establishment establishment, long? trustUid = null, long? federationUid = null,
            int laCode = 0)
        {
            establishment.Trusts = trustUid.HasValue ? new CodeNamePair {Code = trustUid.Value.ToString()} : null;
            establishment.Federations = federationUid.HasValue ? new CodeNamePair {Code = federationUid.Value.ToString()} : null;
            establishment.LA = new CodeNamePair {Code = laCode.ToString()};
        }
    }
}