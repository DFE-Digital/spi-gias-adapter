using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Dfe.Spi.GiasAdapter.Domain;
using Dfe.Spi.GiasAdapter.Functions.LearningProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Functions.UnitTests.LearningProviders
{
    public class WhenGettingLearningProvider
    {
        private Mock<ILearningProviderManager> _learningProviderManagerMock;
        private Mock<ILogger> _loggerMock;
        private GetLearningProvider _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _learningProviderManagerMock = new Mock<ILearningProviderManager>();

            _loggerMock = new Mock<ILogger>();

            _function = new GetLearningProvider(
                _learningProviderManagerMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnLearningProviderIfFound(int urn, LearningProvider provider)
        {
            _learningProviderManagerMock.Setup(x =>
                    x.GetLearningProviderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(provider);

            var actual = await _function.Run(new DefaultHttpRequest(new DefaultHttpContext()), urn.ToString(),
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<OkObjectResult>(actual);
            Assert.AreSame(provider, ((OkObjectResult) actual).Value);
        }

        [Test]
        public async Task ThenItShouldReturnNotFoundResultIfNotFound()
        {
            _learningProviderManagerMock.Setup(x =>
                    x.GetLearningProviderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LearningProvider) null);

            var actual = await _function.Run(new DefaultHttpRequest(new DefaultHttpContext()), "123",
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<NotFoundResult>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnBadRequestResultIfArgumentExceptionThrown(string message)
        {
            _learningProviderManagerMock.Setup(x =>
                    x.GetLearningProviderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException(message));

            var actual = await _function.Run(new DefaultHttpRequest(new DefaultHttpContext()), "123",
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<BadRequestObjectResult>(actual);
            Assert.AreSame(message, ((BadRequestObjectResult) actual).Value);
        }
    }
}