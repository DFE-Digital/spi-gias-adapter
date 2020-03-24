using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.NUnit3;
using Castle.Core.Resource;
using Dfe.Spi.Common.Caching.Definitions;
using Dfe.Spi.Common.Context.Definitions;
using Dfe.Spi.Common.Context.Models;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.WellKnownIdentifiers;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RestSharp;

namespace Dfe.Spi.GiasAdapter.Infrastructure.SpiTranslator.UnitTests
{
    public class WhenTranslatingEnumValue
    {
        private AuthenticationConfiguration _authenticationConfiguration;
        private Mock<IRestClient> _restClientMock;
        private Mock<ICacheProvider> _cacheProviderMock;
        private Mock<ISpiExecutionContextManager> _spiExecutionContextManager;
        private TranslatorConfiguration _configuration;
        private Mock<ILoggerWrapper> _loggerMock;
        private TranslatorApiClient _translator;
        private CancellationToken _cancellationToken;

        [SetUp]
        public void Arrange()
        {
            _authenticationConfiguration = new AuthenticationConfiguration()
            {
                ClientId = "some client id",
                ClientSecret = "some secret",
                Resource = "http://this.doesnt.exist.local/abc",
                TokenEndpoint = "https://somecorp.local/tokens",
            };

            _restClientMock = new Mock<IRestClient>();
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RestResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    ResponseStatus = ResponseStatus.Completed,
                    Content = GetValidResponse("Value1", new[] {"Mapped1"})
                });
            
            _cacheProviderMock = new Mock<ICacheProvider>();

            _spiExecutionContextManager = new Mock<ISpiExecutionContextManager>();
            _spiExecutionContextManager.Setup(x => x.SpiExecutionContext).Returns(new SpiExecutionContext());

            _configuration = new TranslatorConfiguration
            {
                BaseUrl = "https://translator.unit.tests",
            };

            _loggerMock = new Mock<ILoggerWrapper>();

            _translator = new TranslatorApiClient(
                _authenticationConfiguration,
                _restClientMock.Object,
                _cacheProviderMock.Object,
                _spiExecutionContextManager.Object,
                _configuration,
                _loggerMock.Object);

            _cancellationToken = new CancellationToken();
        }

        [Test, AutoData]
        public async Task ThenItShouldCallEnumEndpointForGias(string enumName, string sourceValue)
        {
            await _translator.TranslateEnumValue(enumName, sourceValue, _cancellationToken);

            _restClientMock.Verify(c => c.ExecuteTaskAsync(It.Is<RestRequest>(r =>
                    r.Method == Method.GET &&
                    r.Resource == $"enumerations/{enumName}/{SourceSystemNames.GetInformationAboutSchools}"), _cancellationToken),
                Times.Once);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnMappingFromApiWhereAvailable(string enumName, string sourceValue,
            string sdmValue)
        {
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RestResponse
                {
                    StatusCode = HttpStatusCode.OK,
                    ResponseStatus = ResponseStatus.Completed,
                    Content = GetValidResponse(sdmValue, new[] {sourceValue})
                });

            var actual = await _translator.TranslateEnumValue(enumName, sourceValue, _cancellationToken);
            Assert.AreEqual(sdmValue, actual);
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnNullWhereMappingNotAvailable(string enumName, string sourceValue)
        {
            var actual = await _translator.TranslateEnumValue(enumName, sourceValue, _cancellationToken);

            Assert.IsNull(actual);
        }

        [Test]
        public void ThenItShouldThrowExceptionIfApiReturnsNonSuccess()
        {
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RestResponse
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    ResponseStatus = ResponseStatus.Completed,
                });

            var actual = Assert.ThrowsAsync<TranslatorApiException>(async () =>
                await _translator.TranslateEnumValue("enum", "value", _cancellationToken));
            Assert.AreEqual(HttpStatusCode.InternalServerError, actual.StatusCode);
        }


        private string GetValidResponse(string sdmValue, string[] mappings)
        {
            var obj = new JObject(
                new JProperty("mappingsResult", new JObject(
                    new JProperty("mappings", new JObject(
                        new JProperty(sdmValue, new JArray(mappings)))))));
            return obj.ToString();
        }
    }
}