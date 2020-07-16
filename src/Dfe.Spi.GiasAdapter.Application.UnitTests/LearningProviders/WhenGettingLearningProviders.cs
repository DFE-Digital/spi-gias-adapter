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
    public class WhenGettingLearningProviders
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
        public async Task ThenItShouldGetEstablishmentsFromApiIfReadFromLive(int[] urns)
        {
            await _manager.GetLearningProvidersAsync(urns.Select(x => x.ToString()).ToArray(), null, true, null, _cancellationToken);

            _giasApiClientMock.Verify(c => c.GetEstablishmentAsync(It.IsAny<long>(), _cancellationToken),
                Times.Exactly(urns.Length));
            for (var i = 0; i < urns.Length; i++)
            {
                _giasApiClientMock.Verify(c => c.GetEstablishmentAsync(urns[i], _cancellationToken),
                    Times.Once, $"Expected call for urn {i}");
            }
            _establishmentRepository.Verify(c => c.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test, AutoData]
        public async Task ThenItShouldGetEstablishmentsFromCacheIfNotReadFromLive(int[] urns)
        {
            await _manager.GetLearningProvidersAsync(urns.Select(x => x.ToString()).ToArray(), null, false, null, _cancellationToken);

            _establishmentRepository.Verify(c => c.GetEstablishmentAsync(
                    It.IsAny<long>(), It.IsAny<DateTime?>(), _cancellationToken),
                Times.Exactly(urns.Length));
            for (var i = 0; i < urns.Length; i++)
            {
                _establishmentRepository.Verify(c => c.GetEstablishmentAsync(urns[i], null, _cancellationToken),
                    Times.Once, $"Expected call for urn {i}");
            }
            _giasApiClientMock.Verify(c => c.GetEstablishmentAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        [TestCase(true, null)]
        [TestCase(false, null)]
        [TestCase(true, "2020-07-16")]
        [TestCase(false, "2020-07-16")]
        public void ThenItShouldThrowExceptionIfIdIsNotNumeric(bool readFromLive, DateTime? pointInTime)
        {
            var ids = new[] {"12345678", "NotANumber", "98765432"};
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _manager.GetLearningProvidersAsync(ids, null, readFromLive, pointInTime, _cancellationToken));
        }

        [TestCase(true, null)]
        [TestCase(false, null)]
        [TestCase(true, "2020-07-16")]
        [TestCase(false, "2020-07-16")]
        public async Task ThenItShouldMapEstablishmentsToLearningProviders(bool readFromLive, DateTime? pointInTime)
        {
            var establishments = _fixture.Create<PointInTimeEstablishment[]>();
            var urns = establishments.Select(x => x.Urn).ToArray();
            for (var i = 0; i < establishments.Length; i++)
            {
                _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(establishments[i].Urn, _cancellationToken))
                    .ReturnsAsync(establishments[i]);
                _establishmentRepository.Setup(c => c.GetEstablishmentAsync(establishments[i].Urn, pointInTime, _cancellationToken))
                    .ReturnsAsync(establishments[i]);
            }

            await _manager.GetLearningProvidersAsync(urns.Select(x => x.ToString()).ToArray(), null, readFromLive, pointInTime, _cancellationToken);

            _mapperMock.Verify(m => m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), _cancellationToken),
                Times.Exactly(establishments.Length));
            for (var i = 0; i < establishments.Length; i++)
            {
                _mapperMock.Verify(m => m.MapAsync<LearningProvider>(establishments[i], _cancellationToken),
                    Times.Once, $"Expected to map establishment at index {i} exactly once");
            }
        }

        [TestCase(true, null)]
        [TestCase(false, null)]
        [TestCase(true, "2020-07-16")]
        [TestCase(false, "2020-07-16")]
        public async Task ThenItShouldReturnMappedLearningProviders(bool readFromLive, DateTime? pointInTime)
        {
            var learningProviders = _fixture.Create<LearningProvider[]>();
            var urns = learningProviders.Select(x => x.Urn.Value).ToArray();
            for (var i = 0; i < learningProviders.Length; i++)
            {
                var establishment = new PointInTimeEstablishment()
                {
                    Urn = learningProviders[i].Urn.Value,
                };
                
                _giasApiClientMock.Setup(c => c.GetEstablishmentAsync(establishment.Urn, _cancellationToken))
                    .ReturnsAsync(establishment);
                _establishmentRepository.Setup(c => c.GetEstablishmentAsync(establishment.Urn, pointInTime, _cancellationToken))
                    .ReturnsAsync(establishment);
                _mapperMock.Setup(m => m.MapAsync<LearningProvider>(It.IsAny<Establishment>(), _cancellationToken))
                    .ReturnsAsync((Establishment e, CancellationToken ct) => learningProviders.Single(x => x.Urn == e.Urn));
            }
            
            var actual = await _manager.GetLearningProvidersAsync(urns.Select(x => x.ToString()).ToArray(), null, readFromLive, pointInTime, _cancellationToken);
            
            Assert.AreEqual(learningProviders.Length, actual.Length);
            for (var i = 0; i < learningProviders.Length; i++)
            {
                Assert.AreSame(learningProviders[i], actual[i],
                    $"Expected {i} to be same");
            }
        }
    }
}