using System.Xml.Linq;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi
{
    internal class GetEstablishmentMessageBuilder : GiasSoapMessageBuilder<GetEstablishmentRequest>
    {
        public GetEstablishmentMessageBuilder(string username, string password, int messageValidForSeconds = 30)
            : base(username, password, messageValidForSeconds)
        {
        }

        protected override XElement BuildBody(GetEstablishmentRequest parameters)
        {
            return new XElement(soapNs + "Body",
                new XElement(giasNs + "GetEstablishment",
                    new XElement(giasNs + "Urn", parameters.Urn)));
        }
    }
}