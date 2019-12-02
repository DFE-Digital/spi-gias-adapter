using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.GiasAdapter.Domain;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.PocoMapping;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Infrastructure.InProcMapping.UnitTests.PocoMapping
{
    public class WhenMappingGenericObject
    {
        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProviderWhenMappingEstablishmentToLearningProvider(Establishment source)
        {
            var mapper = new PocoMapper();

            var actual = await mapper.MapAsync<LearningProvider>(source, new CancellationToken());
            
            Assert.IsInstanceOf<LearningProvider>(actual);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfNoMapperDefined()
        {
            var mapper = new PocoMapper();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await mapper.MapAsync<Establishment>(new object(), new CancellationToken()));
        }
    }
}