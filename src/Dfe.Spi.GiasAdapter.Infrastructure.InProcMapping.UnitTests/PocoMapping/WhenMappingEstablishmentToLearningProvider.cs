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
    public class WhenMappingEstablishmentToLearningProvider
    {
        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProvider(Establishment source)
        {
            var mapper = new EstablishmentMapper();

            var actual = await mapper.MapAsync<LearningProvider>(source, new CancellationToken());

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<LearningProvider>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldMapEstablishmentToLearningProvider(Establishment source)
        {
            var mapper = new EstablishmentMapper();

            var actual = await mapper.MapAsync<LearningProvider>(source, new CancellationToken()) as LearningProvider;

            Assert.IsNotNull(actual);
            Assert.AreEqual(source.Name, actual.Name);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfSourceIsNotEstablishment()
        {
            var mapper = new EstablishmentMapper();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await mapper.MapAsync<LearningProvider>(new object(), new CancellationToken()));
        }

        [Test]
        public void ThenItShouldThrowExceptionIfDestinationIsNotLearningProvider()
        {
            var mapper = new EstablishmentMapper();

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await mapper.MapAsync<object>(new Establishment(), new CancellationToken()));
        }
    }
}