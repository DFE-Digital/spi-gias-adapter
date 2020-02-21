using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
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
        private EstablishmentMapper _mapper;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _translatorMock = new Mock<ITranslator>();
            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ManagementGroupType, LocalAuthority.ManagementGroupType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(LocalAuthorityManagementGroupType);

            _mapper = new EstablishmentMapper(_translatorMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProvider(Establishment source)
        {
            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<LearningProvider>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapEstablishmentToLearningProviderForBasicTypeProperties(Establishment source)
        {
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
        public async Task ThenItShouldMapManagementGroupToLAIfNoLinks(Establishment source)
        {
            source.GroupLinks = null;
            
            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);
            
            Assert.IsNotNull(actual.ManagementGroup);
            Assert.AreEqual(source.LA.Code, actual.ManagementGroup.Identifier);
            Assert.AreEqual(source.LA.DisplayName, actual.ManagementGroup.Name);
            Assert.AreEqual(LocalAuthorityManagementGroupType, actual.ManagementGroup.Type);
            Assert.AreEqual($"{LocalAuthorityManagementGroupType}-{source.LA.Code}", actual.ManagementGroup.Code);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapManagementGroupToLAIfLinkIsOnlySponsor(Establishment source)
        {
            source.GroupLinks = new[]
            {
                new GroupLink
                {
                    GroupType = "School sponsor",
                    Uid = 123456,
                    Urn = source.Urn,   
                }, 
            };
            
            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);
            
            Assert.IsNotNull(actual.ManagementGroup);
            Assert.AreEqual(source.LA.Code, actual.ManagementGroup.Identifier);
            Assert.AreEqual(source.LA.DisplayName, actual.ManagementGroup.Name);
            Assert.AreEqual(LocalAuthorityManagementGroupType, actual.ManagementGroup.Type);
            Assert.AreEqual($"{LocalAuthorityManagementGroupType}-{source.LA.Code}", actual.ManagementGroup.Code);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapManagementGroupFromLinkWhenLinkIsGroup(Establishment source, long uid)
        {
            source.GroupLinks = new[]
            {
                new GroupLink
                {
                    GroupType = "Single-academy trust",
                    Uid = uid,
                    Urn = source.Urn,   
                }, 
            };
            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ManagementGroupType, "Single-academy trust", It.IsAny<CancellationToken>()))
                .ReturnsAsync("SAT");
            
            var actual = await _mapper.MapAsync<LearningProvider>(source, _cancellationToken);
            
            Assert.IsNotNull(actual.ManagementGroup);
            Assert.AreEqual(uid.ToString(), actual.ManagementGroup.Identifier);
            Assert.AreEqual("SAT", actual.ManagementGroup.Type);
            Assert.AreEqual($"SAT-{uid}", actual.ManagementGroup.Code);
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
    }
}