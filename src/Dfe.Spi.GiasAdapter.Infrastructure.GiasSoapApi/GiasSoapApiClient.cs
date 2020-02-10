using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using RestSharp;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi
{
    public class GiasSoapApiClient : IGiasApiClient
    {
        private readonly IRestClient _restClient;
        private IGiasSoapMessageBuilder<GetEstablishmentRequest> _getEstablishmentMessageBuilder;

        internal GiasSoapApiClient(IRestClient restClient,
            IGiasSoapMessageBuilder<GetEstablishmentRequest> getEstablishmentMessageBuilder)
        {
            _restClient = restClient;
            _getEstablishmentMessageBuilder = getEstablishmentMessageBuilder;
        }

        public GiasSoapApiClient(GiasApiConfiguration configuration)
            : this(new RestClient(configuration.Url),
                new GetEstablishmentMessageBuilder(configuration.Username, configuration.Password))
        {
        }

        public async Task<Establishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken)
        {
            var message = _getEstablishmentMessageBuilder.Build(new GetEstablishmentRequest
            {
                Urn = urn,
            });

            var request = new RestRequest(Method.POST);
            request.AddParameter("text/xml", message, ParameterType.RequestBody);
            request.AddHeader("SOAPAction", "http://ws.edubase.texunatech.com/GetEstablishment");

            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            try
            {
                var result = EnsureSuccessResponseAndExtractResult(response);

                var root = result.GetElementByLocalName("Establishment");

                var establishment = new Establishment
                {
                    Urn = urn,

                    EstablishmentTypeGroup = root.GetCodeNamePairFromChildElement("EstablishmentTypeGroup"),
                    TypeOfEstablishment = root.GetCodeNamePairFromChildElement("TypeOfEstablishment"),
                    EstablishmentStatus = root.GetCodeNamePairFromChildElement("EstablishmentStatus"),
                    OpenDate = root.GetDateTimeFromChildElement("OpenDate"),
                    CloseDate = root.GetDateTimeFromChildElement("CloseDate"),
                    LA = root.GetCodeNamePairFromChildElement("LA"),
                    Postcode = root.GetValueFromChildElement("Postcode"),
                    EstablishmentName = root.GetValueFromChildElement("EstablishmentName"),
                    Ukprn = root.GetLongFromChildElement("UKRPN"),
                    Uprn = root.GetValueFromChildElement("UPRN"),
                    Trusts = root.GetCodeNamePairFromChildElement("Trusts"),
                    EstablishmentNumber = root.GetLongFromChildElement("EstablishmentNumber"),
                    PreviousEstablishmentNumber = root.GetLongFromChildElement("PreviousEstablishmentNumber"),
                };

                return establishment;
            }
            catch (SoapException ex)
            {
                // GIAS API throws exception if not found for some reason
                // This is the only obvious way to identify this error case
                if (ex.Message == "Unknown URN")
                {
                    return null;
                }

                throw;
            }
        }

        public Task<Establishment[]> DownloadEstablishmentsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static XElement EnsureSuccessResponseAndExtractResult(IRestResponse response)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse(response.Content);
            }
            catch (Exception ex)
            {
                throw new GiasSoapApiException(
                    $"Error deserializing SOAP response: {ex.Message} (response: {response.Content})", ex);
            }

            var envelope = document.Elements().Single();
            var body = envelope.GetElementByLocalName("Body");

            if (!response.IsSuccessful)
            {
                var fault = body.Elements().Single();
                var faultCode = fault.GetElementByLocalName("faultcode");
                var faultString = fault.GetElementByLocalName("faultstring");
                throw new SoapException(faultCode.Value, faultString.Value);
            }

            return body.Elements().First();
        }
    }
}