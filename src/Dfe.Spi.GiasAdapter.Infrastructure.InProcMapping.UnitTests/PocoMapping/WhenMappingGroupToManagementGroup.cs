using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.Models.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.UnitTests.PocoMapping
{
    public class WhenMappingGroupToManagementGroup
    {
        private Mock<ITranslator> _translatorMock;
        private GroupMapper _mapper;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _translatorMock = new Mock<ITranslator>();

            _mapper = new GroupMapper(_translatorMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnManagementGroup(Group source)
        {
            var actual = await _mapper.MapAsync<ManagementGroup>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<ManagementGroup>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapGroupToManagementGroupForBasicTypeProperties(Group source)
        {
            var actual = await _mapper.MapAsync<ManagementGroup>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual(source.GroupName, actual.Name);
            Assert.AreEqual(source.Uid.ToString(), actual.Identifier);
            Assert.AreEqual(source.CompaniesHouseNumber, actual.CompaniesHouseNumber);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapStatusFromTranslation(Group source, string transformedValue)
        {
            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ManagementGroupType, It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(transformedValue);
        
            var actual = await _mapper.MapAsync<ManagementGroup>(source, _cancellationToken);
        
            Assert.IsNotNull(actual);
            Assert.AreEqual(transformedValue, actual.Type);
            _translatorMock.Verify(
                t => t.TranslateEnumValue(EnumerationNames.ManagementGroupType, source.GroupType,
                    _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapCodeFromTranslatedTypeAndIdentifier(Group source, string transformedType)
        {
            _translatorMock.Setup(t =>
                    t.TranslateEnumValue(EnumerationNames.ManagementGroupType, It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(transformedType);
            
            var actual = await _mapper.MapAsync<ManagementGroup>(source, _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.AreEqual($"{transformedType}-{source.Uid}", actual.Code);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfSourceIsNotGroup()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _mapper.MapAsync<ManagementGroup>(new object(), new CancellationToken()));
        }

        [Test]
        public void ThenItShouldThrowExceptionIfDestinationIsNotManagementGroup()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _mapper.MapAsync<object>(new Group(), new CancellationToken()));
        }
    }
}