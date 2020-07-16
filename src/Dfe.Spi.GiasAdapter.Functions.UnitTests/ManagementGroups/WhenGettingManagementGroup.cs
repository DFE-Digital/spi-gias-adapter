using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.UnitTesting.Fixtures;
using Dfe.Spi.GiasAdapter.Application.ManagementGroups;
using Dfe.Spi.GiasAdapter.Functions.ManagementGroups;
using Dfe.Spi.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Functions.UnitTests.ManagementGroups
{
    public class WhenGettingManagementGroup
    {
        private Mock<IHttpSpiExecutionContextManager> _httpSpiExecutionContextManagerMock;
        private Mock<IManagementGroupManager> _managementGroupManagerMock;
        private Mock<ILoggerWrapper> _loggerMock;
        private GetManagementGroup _function;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _managementGroupManagerMock = new Mock<IManagementGroupManager>();

            _httpSpiExecutionContextManagerMock = new Mock<IHttpSpiExecutionContextManager>();

            _loggerMock = new Mock<ILoggerWrapper>();

            _function = new GetManagementGroup(
                _httpSpiExecutionContextManagerMock.Object,
                _managementGroupManagerMock.Object,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, NonRecursiveAutoData]
        public async Task ThenItShouldReturnLearningProviderIfFound(string code, ManagementGroup managementGroup)
        {
            _managementGroupManagerMock.Setup(x =>
                    x.GetManagementGroupAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(managementGroup);

            var actual = await _function.RunAsync(new DefaultHttpRequest(new DefaultHttpContext()), code,
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<FormattedJsonResult>(actual);
            Assert.AreSame(managementGroup, ((FormattedJsonResult) actual).Value);
        }

        [Test]
        public async Task ThenItShouldReturnNotFoundResultIfNotFound()
        {
            _managementGroupManagerMock.Setup(x =>
                    x.GetManagementGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ManagementGroup) null);

            var actual = await _function.RunAsync(new DefaultHttpRequest(new DefaultHttpContext()), "LA-123",
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<NotFoundResult>(actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnBadRequestResultIfArgumentExceptionThrown(string message)
        {
            _managementGroupManagerMock.Setup(x =>
                    x.GetManagementGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException(message));

            var actual = await _function.RunAsync(new DefaultHttpRequest(new DefaultHttpContext()), "LA-123",
                _cancellationToken);

            Assert.IsNotNull(actual);
            Assert.IsInstanceOf<BadRequestObjectResult>(actual);
            Assert.AreSame(message, ((BadRequestObjectResult) actual).Value);
        }
    }
}