using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;
using Dfe.Spi.Models.Entities;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Application.UnitTests.LearningProviders
{
    public class WhenGettingLearningProvider
    {
        private Fixture _fixture;
        private Mock<IGiasApiClient> _giasApiClientMock;
        private Mock<IEstablishmentRepository> _establishmentRepository;
        private Mock<IMapper> _mapperMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private LearningProviderManager _manager;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior()); // recursionDepth
            
            _giasApiClientMock = new Mock<IGiasApiClient>();

            _establishmentRepository = new Mock<IEstablishmentRepository>();

            _mapperMock = new Mock<IMapper>();
            
            _loggerMock = new Mock<ILoggerWrapper>();

            _manager = new LearningProviderManager(
                _giasApiClientMock.Object, 
                _establishmentRepository.Object,
                _mapperMock.Object, 
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldGetEstablishmentFromApiIfReadFromLive(int urn, string fields)
        {
            await _manager.GetLearningProviderAsync(urn.ToString(), fields, true, null, _cancellationToken);

            _giasApiClientMock.Verify(c => c.GetEstablishmentAsync(urn, _cancellationToken),
                Times.Once);
            _establishmentRepository.Verify(c => c.GetEstablishmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNullIfEstablishmentNotFoundOnApiIfReadFromLive(int urn, string fields)
        {
            _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(urn, _cancellationToken))
                .ReturnsAsync((Establishment) null);

            var actual = await _manager.GetLearningProviderAsync(urn.ToString(), fields, true, null, _cancellationToken);

            Assert.IsNull(actual);
        }
        
        [Test, AutoData]
        public async Task ThenItShouldGetEstablishmentFromCacheIfNotReadFromLive(int urn, string fields)
        {
            await _manager.GetLearningProviderAsync(urn.ToString(), fields, false, null, _cancellationToken);

            _establishmentRepository.Verify(c=>c.GetEstablishmentAsync(urn, null, _cancellationToken),
                Times.Once);
            _giasApiClientMock.Verify(c => c.GetEstablishmentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNullIfEstablishmentNotFoundInCacheIfNotReadFromLive(int urn, string fields)
        {
            _establishmentRepository.Setup(c => c.GetEstablishmentAsync(urn, null, _cancellationToken))
                .ReturnsAsync((PointInTimeEstablishment) null);

            var actual = await _manager.GetLearningProviderAsync(urn.ToString(), fields, true, null, _cancellationToken);

            Assert.IsNull(actual);
        }
        
        [TestCase(true, null)]
        [TestCase(false, null)]
        [TestCase(true, "2020-07-16")]
        [TestCase(false, "2020-07-16")]
        public void ThenItShouldThrowExceptionIfIdIsNotNumeric(bool readFromLive, DateTime? pointInTime)
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _manager.GetLearningProviderAsync("NotANumber", null, readFromLive, pointInTime, _cancellationToken));
        }

        [TestCase(true, null)]
        [TestCase(false, null)]
        [TestCase(true, "2020-07-16")]
        [TestCase(false, "2020-07-16")]
        public async Task ThenItShouldMapEstablishmentToLearningProvider(bool readFromLive, DateTime? pointInTime)
        {
            var urn = _fixture.Create<int>();
            var establishment = _fixture.Create<PointInTimeEstablishment>();
            
            _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(urn, _cancellationToken))
                .ReturnsAsync(establishment);
            _establishmentRepository.Setup(c => c.GetEstablishmentAsync(urn, pointInTime, _cancellationToken))
                .ReturnsAsync(establishment);

            await _manager.GetLearningProviderAsync(urn.ToString(), null, readFromLive, pointInTime, _cancellationToken);

            _mapperMock.Verify(m => m.MapAsync<LearningProvider>(establishment, _cancellationToken),
                Times.Once);
        }

        [TestCase(true, null)]
        [TestCase(false, null)]
        [TestCase(true, "2020-07-16")]
        [TestCase(false, "2020-07-16")]
        public async Task ThenItShouldReturnMappedLearningProvider(bool readFromLive, DateTime? pointInTime)
        {
            var urn = _fixture.Create<int>();
            var learningProvider = _fixture.Create<LearningProvider>();
            
            _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(urn, _cancellationToken))
                .ReturnsAsync(new Establishment());
            _establishmentRepository.Setup(c => c.GetEstablishmentAsync(urn, pointInTime, _cancellationToken))
                .ReturnsAsync(new PointInTimeEstablishment());
            _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), _cancellationToken))
                .ReturnsAsync(learningProvider);

            var actual = await _manager.GetLearningProviderAsync(urn.ToString(), null, readFromLive, pointInTime, _cancellationToken);

            Assert.AreSame(learningProvider, actual);
        }
    }
}