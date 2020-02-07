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

                var establishmentElement = result.GetElementByLocalName("Establishment");

                var establishment = new Establishment
                {
                    Urn = urn,
                    Name = establishmentElement.GetElementByLocalName("EstablishmentName").Value,
                    Postcode = establishmentElement.GetElementByLocalName("Postcode")?.Value,
                    EstablishmentStatus =
                        GetCodeNamePairFromElement(establishmentElement.GetElementByLocalName("EstablishmentStatus")),
                    EstablishmentTypeGroup =
                        GetCodeNamePairFromElement(
                            establishmentElement.GetElementByLocalName("EstablishmentTypeGroup")),
                    TypeOfEstablishment =
                        GetCodeNamePairFromElement(establishmentElement.GetElementByLocalName("TypeOfEstablishment")),
                    OpenDate = GetDateTimeFromElement(establishmentElement.GetElementByLocalName("OpenDate")),
                    CloseDate = GetDateTimeFromElement(establishmentElement.GetElementByLocalName("CloseDate")),
                };

                var ukprnElement = establishmentElement.GetElementByLocalName("UKPRN");
                if (ukprnElement != null && !string.IsNullOrEmpty(ukprnElement.Value))
                {
                    establishment.Ukprn = long.Parse(ukprnElement.Value);
                }

                var uprnElement = establishmentElement.GetElementByLocalName("UPRN");
                if (uprnElement != null && !string.IsNullOrEmpty(uprnElement.Value))
                {
                    establishment.Uprn = uprnElement.Value;
                }

                var trustElement = establishmentElement.GetElementByLocalName("Trusts");
                if (trustElement != null && !string.IsNullOrEmpty(trustElement.Value))
                {
                    establishment.AcademyTrustCode = trustElement.GetElementByLocalName("Value").GetElementByLocalName("Code").Value;
                }

                var laElement = establishmentElement.GetElementByLocalName("LA");
                if (laElement != null && !string.IsNullOrEmpty(laElement.Value))
                {
                    establishment.LocalAuthorityCode = laElement.GetElementByLocalName("Code").Value;
                }

                var establishmentNumberElement = establishmentElement.GetElementByLocalName("EstablishmentNumber");
                if (establishmentNumberElement != null && !string.IsNullOrEmpty(establishmentNumberElement.Value))
                {
                    establishment.EstablishmentNumber = long.Parse(establishmentNumberElement.Value);
                }

                var previousEstablishmentNumberElement = establishmentElement.GetElementByLocalName("PreviousEstablishmentNumber");
                if (previousEstablishmentNumberElement != null && !string.IsNullOrEmpty(previousEstablishmentNumberElement.Value))
                {
                    establishment.PreviousEstablishmentNumber = long.Parse(previousEstablishmentNumberElement.Value);
                }

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

        private static CodeNamePair GetCodeNamePairFromElement(XElement element)
        {
            if (element == null)
            {
                return null;
            }

            return new CodeNamePair
            {
                Code = int.Parse(element.GetElementByLocalName("Code").Value),
                DisplayName = element.GetElementByLocalName("DisplayName").Value,
            };
        }

        private static DateTime? GetDateTimeFromElement(XElement element)
        {
            if (element == null || string.IsNullOrEmpty(element.Value))
            {
                return null;
            }

            return DateTime.Parse(element.Value);
        }
    }
}