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
            var result = EnsureSuccessResponseAndExtractResult(response);

            var establishment = result.GetElementByLocalName("Establishment");

            return new Establishment
            {
                Urn = urn,
                Name = establishment.GetElementByLocalName("EstablishmentName").Value,
            };
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
                throw new GiasSoapApiException($"Error deserializing SOAP response: {ex.Message} (response: {response.Content})", ex);
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