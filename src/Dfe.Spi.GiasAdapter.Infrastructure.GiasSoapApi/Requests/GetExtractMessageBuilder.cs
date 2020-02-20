using System.Xml.Linq;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.Requests
{
    internal class GetExtractMessageBuilder : GiasSoapMessageBuilder<GetExtractRequest>
    {
        public GetExtractMessageBuilder(string username, string password, int messageValidForSeconds = 30)
            : base(username, password, messageValidForSeconds)
        {
        }

        protected override XElement BuildBody(GetExtractRequest parameters)
        {
            return new XElement(soapNs + "Body",
                new XElement(giasNs + "GetExtract",
                    new XElement(giasNs + "Id", parameters.ExtractId)));
        }
    }
}