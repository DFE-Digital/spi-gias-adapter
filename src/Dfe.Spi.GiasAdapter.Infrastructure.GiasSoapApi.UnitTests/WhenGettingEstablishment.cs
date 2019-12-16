using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoFixture.NUnit3;
using Moq;
using NUnit.Framework;
using RestSharp;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.UnitTests
{
    public class WhenGettingEstablishment
    {
        private Mock<IRestClient> _restClientMock;
        private Mock<IGiasSoapMessageBuilder<GetEstablishmentRequest>> _getEstablishmentMessageBuilderMock;
        private GiasSoapApiClient _client;

        [SetUp]
        public void Arrange()
        {
            _restClientMock = new Mock<IRestClient>();
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetValidResponse(123, "Test"));

            _getEstablishmentMessageBuilderMock = new Mock<IGiasSoapMessageBuilder<GetEstablishmentRequest>>();
            _getEstablishmentMessageBuilderMock.Setup(b => b.Build(It.IsAny<GetEstablishmentRequest>()))
                .Returns("some-soap-xml-request");

            _client = new GiasSoapApiClient(_restClientMock.Object, _getEstablishmentMessageBuilderMock.Object);
        }

        [Test, AutoData]
        public async Task ThenItShouldBuildMessageUsingRequestUrn(long urn)
        {
            await _client.GetEstablishmentAsync(urn, new CancellationToken());

            _getEstablishmentMessageBuilderMock.Verify(b => b.Build(It.Is<GetEstablishmentRequest>(r => r.Urn == urn)));
        }

        [Test, AutoData]
        public async Task ThenItShouldExecuteSoapRequestAgainstServer(long urn, string soapRequestMessage)
        {
            _getEstablishmentMessageBuilderMock.Setup(b => b.Build(It.IsAny<GetEstablishmentRequest>()))
                .Returns(soapRequestMessage);

            await _client.GetEstablishmentAsync(urn, new CancellationToken());

            var expectedSoapAction = "http://ws.edubase.texunatech.com/GetEstablishment";
            _restClientMock.Verify(c => c.ExecuteTaskAsync(It.Is<IRestRequest>(r =>
                    r.Method == Method.POST &&
                    r.Parameters.Any(p => p.Name == "SOAPAction") &&
                    (string) r.Parameters.Single(p => p.Name == "SOAPAction").Value == expectedSoapAction &&
                    r.Parameters.Any(p => p.Type == ParameterType.RequestBody) &&
                    r.Parameters.Single(p => p.Type == ParameterType.RequestBody).Value == soapRequestMessage),
                It.IsAny<CancellationToken>()));
        }

        [Test, AutoData]
        public async Task ThenItShouldReturnDeserializedEstablishment(long urn, string establishmentName)
        {
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetValidResponse(urn, establishmentName));

            var actual = await _client.GetEstablishmentAsync(urn, new CancellationToken());

            Assert.IsNotNull(actual);
            Assert.AreEqual(urn, actual.Urn);
            Assert.AreEqual(establishmentName, actual.Name);
        }

        [Test, AutoData]
        public void ThenItShouldThrowExceptionIfSoapFaultReceived(long urn, string faultCode, string faultString)
        {
            _restClientMock.Setup(c => c.ExecuteTaskAsync(It.IsAny<IRestRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(GetFaultResponse(faultCode, faultString));

            var actual = Assert.ThrowsAsync<SoapException>(async () =>
                await _client.GetEstablishmentAsync(urn, new CancellationToken()));
            Assert.AreEqual(faultCode, actual.FaultCode);
            Assert.AreEqual(faultString, actual.Message);
        }


        private XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";

        private IRestResponse GetValidResponse(long urn, string establishmentName)
        {
            XNamespace giasNs = "http://ws.edubase.texunatech.com";
            XNamespace establishmentNs = "http://ws.edubase.texunatech.com/Establishment";

            var envelope = GetSoapEnvelope(new XElement(giasNs + "GetEstablishmentResponse",
                new XElement(giasNs + "Establishment",
                    new XElement(establishmentNs + "URN", urn),
                    new XElement(establishmentNs + "EstablishmentName", establishmentName))));

            var responseMock = new Mock<IRestResponse>();
            responseMock.Setup(r => r.Content).Returns(envelope.ToString());
            responseMock.Setup(r => r.IsSuccessful).Returns(true);
            return responseMock.Object;
        }

        private IRestResponse GetFaultResponse(string faultCode, string faultString)
        {
            var envelope = GetSoapEnvelope(new XElement(soapNs + "Fault",
                new XElement("faultcode", faultCode),
                new XElement("faultstring", faultString)));

            var responseMock = new Mock<IRestResponse>();
            responseMock.Setup(r => r.Content).Returns(envelope.ToString());
            responseMock.Setup(r => r.IsSuccessful).Returns(false);
            return responseMock.Object;
        }

        private XElement GetSoapEnvelope(XElement bodyContent)
        {
            return new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", soapNs.NamespaceName),
                new XElement(soapNs + "Body",
                    bodyContent));
        }
    }
}