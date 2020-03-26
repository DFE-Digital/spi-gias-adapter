using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Castle.Components.DictionaryAdapter;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Translation;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using Dfe.Spi.Models.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.UnitTests.PocoMapping
{
    public class WhenMappingGenericObject
    {
        private Mock<ITranslator> _translatorMock;
        private Mock<IGroupRepository> _groupRepositoryMock;
        private Mock<ILocalAuthorityRepository> _localAuthorityRepositoryMock;
        private PocoMapper _mapper;

        [SetUp]
        public void Arrange()
        {
            _translatorMock = new Mock<ITranslator>();

            _groupRepositoryMock = new Mock<IGroupRepository>();

            _localAuthorityRepositoryMock = new Mock<ILocalAuthorityRepository>();
            _localAuthorityRepositoryMock.Setup(r =>
                    r.GetLocalAuthorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocalAuthority());

            _mapper = new PocoMapper(
                _translatorMock.Object,
                _groupRepositoryMock.Object,
                _localAuthorityRepositoryMock.Object);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProviderWhenMappingEstablishmentToLearningProvider(
            Establishment source)
        {
            source.Trusts = null;
            source.Federations = null;
            source.LA.Code = "123";
            
            var actual = await _mapper.MapAsync<LearningProvider>(source, new CancellationToken());

            Assert.IsInstanceOf<LearningProvider>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnManagementGroupWhenMappingGroupToManagementGroup(Group source)
        {
            var actual = await _mapper.MapAsync<ManagementGroup>(source, new CancellationToken());

            Assert.IsInstanceOf<ManagementGroup>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnManagementGroupWhenMappingLocalAuthorityToManagementGroup(
            LocalAuthority source)
        {
            var actual = await _mapper.MapAsync<ManagementGroup>(source, new CancellationToken());

            Assert.IsInstanceOf<ManagementGroup>(actual);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfNoMapperDefined()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _mapper.MapAsync<Establishment>(new object(), new CancellationToken()));
        }
    }
}