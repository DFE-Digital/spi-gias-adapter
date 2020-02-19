using System.Xml.Linq;
using AutoFixture.NUnit3;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.Requests;
using NUnit.Framework;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.UnitTests.Requests
{
    public class WhenBuildingGetExtractMessage
    {
        [Test, AutoData]
        public void ThenItShouldReturnMessageForExtractId(int extractId, string username, string password)
        {
            // Arrange
            var builder = new GetExtractMessageBuilder(username, password);

            // Act
            var actual = builder.Build(new GetExtractRequest
            {
                ExtractId = extractId,
            });
            
            // Assert
            Assert.IsNotNull(actual);
            
            var body = XElement.Parse(actual).GetElementByLocalName("Body");
            Assert.IsNotNull(body);
            
            var request = body.GetElementByLocalName("GetExtract");
            Assert.IsNotNull(request);
            
            var requestUrn = request.GetElementByLocalName("Id");
            Assert.IsNotNull(requestUrn);
            Assert.AreEqual(requestUrn.Value, extractId.ToString());
        }

        [Test, AutoData]
        public void ThenItShouldReturnMessageThatIsSecured(int extractId, string username, string password)
        {
            // Arrange
            var builder = new GetExtractMessageBuilder(username, password);

            // Act
            var actual = builder.Build(new GetExtractRequest
            {
                ExtractId = extractId,
            });
            
            // Asser
            Assert.IsNotNull(actual);
            
            var header = XElement.Parse(actual).GetElementByLocalName("Header");
            Assert.IsNotNull(header);
            
            var security = header.GetElementByLocalName("Security");
            Assert.IsNotNull(security);
            
            var wsseNs = security.Attribute(XNamespace.Xmlns + "wsse");
            Assert.IsNotNull(wsseNs);
            Assert.AreEqual("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd", wsseNs.Value);
            
            var wsuNs = security.Attribute(XNamespace.Xmlns + "wsu");
            Assert.IsNotNull(wsuNs);
            Assert.AreEqual("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd", wsuNs.Value);

            var usernameToken = security.GetElementByLocalName("UsernameToken");
            Assert.IsNotNull(usernameToken);
            Assert.AreEqual(username, usernameToken.GetElementByLocalName("Username")?.Value);
            Assert.AreEqual(password, usernameToken.GetElementByLocalName("Password")?.Value);
            Assert.AreEqual("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText", 
                usernameToken.GetElementByLocalName("Password")?.Attribute("Type")?.Value);
        }
    }
}