using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
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
        private PocoMapper _mapper;
        
        [SetUp]
        public void Arrange()
        {
            _translatorMock= new Mock<ITranslator>();
            
            _mapper = new PocoMapper(_translatorMock.Object);
        }
        
        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProviderWhenMappingEstablishmentToLearningProvider(Establishment source)
        {
            var actual = await _mapper.MapAsync<LearningProvider>(source, new CancellationToken());
            
            Assert.IsInstanceOf<LearningProvider>(actual);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfNoMapperDefined()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _mapper.MapAsync<Establishment>(new object(), new CancellationToken()));
        }
    }
}