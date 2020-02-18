using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Application.UnitTests.LearningProviders
{
    public class WhenGettingLearningProvider
    {
        private Mock<IGiasApiClient> _giasApiClientMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private LearningProviderManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _giasApiClientMock = new Mock<IGiasApiClient>();

            _mapperMock = new Mock<IMapper>();
            
            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new LearningProviderManager(_giasApiClientMock.Object, _mapperMock.Object, _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldGetEstablishmentFromApi(int urn, string fields)
        {
            await _manager.GetLearningProviderAsync(urn.ToString(), fields, _cancellationToken);

            _giasApiClientMock.Verify(c => c.GetEstablishmentAsync(urn, _cancellationToken),
                Times.Once);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfIdIsNotNumeric()
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _manager.GetLearningProviderAsync("NotANumber", null, _cancellationToken));
        }

        [Test, AutoData]
        public async Task ThenItShouldMapEstablishmentToLearningProvider(int urn, Establishment establishment)
        {
            _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(urn, _cancellationToken))
                .ReturnsAsync(establishment);

            await _manager.GetLearningProviderAsync(urn.ToString(), null, _cancellationToken);

            _mapperMock.Verify(m => m.MapAsync<LearningProvider>(establishment, _cancellationToken),
                Times.Once);
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldReturnMappedLearningProvider(int urn, LearningProvider learningProvider)
        {
            _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(urn, _cancellationToken))
                .ReturnsAsync(new Establishment());
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), _cancellationToken))
                .ReturnsAsync(learningProvider);

            var actual = await _manager.GetLearningProviderAsync(urn.ToString(), null, _cancellationToken);

            Assert.AreSame(learningProvider, actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNullIfEstablishmentNotFoundOnApi(int urn, string fields)
        {
            _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(urn, _cancellationToken))
                .ReturnsAsync((Establishment) null);

            var actual = await _manager.GetLearningProviderAsync(urn.ToString(), fields, _cancellationToken);

            Assert.IsNull(actual);
        }
    }
}