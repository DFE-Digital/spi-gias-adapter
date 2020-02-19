using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.Requests;
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
                var result = EnsureSuccessResponseAndExtractResult(response).Result;

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
                    Ukprn = root.GetLongFromChildElement("UKPRN"),
                    Uprn = root.GetValueFromChildElement("UPRN"),
                    Trusts = root.GetElementByLocalName("Trusts")?.GetCodeNamePairFromChildElement("Value"),
                    EstablishmentNumber = root.GetLongFromChildElement("EstablishmentNumber"),
                    PreviousEstablishmentNumber = root.GetLongFromChildElement("PreviousEstablishmentNumber"),

                    Boarders = root.GetCodeNamePairFromChildElement("Boarders"),
                    StatutoryLowAge = root.GetLongFromChildElement("StatutoryLowAge"),
                    StatutoryHighAge = root.GetLongFromChildElement("StatutoryHighAge"),
                    SchoolWebsite = root.GetValueFromChildElement("SchoolWebsite"),
                    Gender = root.GetCodeNamePairFromChildElement("Gender"),
                    PercentageFsm = root.GetDecimalFromChildElement("PercentageFSM"),
                    OfstedLastInsp = root.GetDateTimeFromChildElement("OfstedLastInsp"),
                    LastChangedDate = root.GetDateTimeFromChildElement("LastChangedDate"),
                    DateOfLastInspectionVisit = root.GetDateTimeFromChildElement("DateOfLastInspectionVisit"),
                    OfstedRating = root.GetCodeNamePairFromChildElement("OfstedRating"),
                    AdmissionsPolicy = root.GetCodeNamePairFromChildElement("AdmissionsPolicy"),
                    InspectorateName = root.GetCodeNamePairFromChildElement("InspectorateName"),
                    InspectorateReport = root.GetValueFromChildElement("InspectorateReport"),
                    TeenMoth = root.GetCodeNamePairFromChildElement("TeenMoth"),
                    TeenMothPlaces = root.GetLongFromChildElement("TeenMothPlaces"),
                    ReasonEstablishmentOpened = root.GetCodeNamePairFromChildElement("ReasonEstablishmentOpened"),
                    ReasonEstablishmentClosed = root.GetCodeNamePairFromChildElement("ReasonEstablishmentClosed"),
                    PhaseOfEducation = root.GetCodeNamePairFromChildElement("PhaseOfEducation"),
                    FurtherEducationType = root.GetCodeNamePairFromChildElement("FurtherEducationType"),
                    OfficialSixthForm = root.GetCodeNamePairFromChildElement("OfficialSixthForm"),
                    Diocese = root.GetCodeNamePairFromChildElement("Diocese"),
                    PreviousLA = null, // NOTE: Does not seem to exist in the SOAP response!
                    DistrictAdministrative = root.GetCodeNamePairFromChildElement("DistrictAdministrative"),
                    AdministrativeWard = root.GetCodeNamePairFromChildElement("AdministrativeWard"),
                    Gor = root.GetCodeNamePairFromChildElement("GOR"),
                    Msoa = root.GetCodeNamePairFromChildElement("MSOA"),
                    Lsoa = root.GetCodeNamePairFromChildElement("LSOA"),
                    RscRegion = root.GetCodeNamePairFromChildElement("RSCRegion"),
                    Section41Approved = root.GetCodeNamePairFromChildElement("Section41Approved"),
                    Easting = root.GetLongFromChildElement("Easting"),
                    Northing = root.GetLongFromChildElement("Northing"),
                    ParliamentaryConstituency = root.GetCodeNamePairFromChildElement("ParliamentaryConstituency"),
                    GsslaCode = root.GetCodeNamePairFromChildElement("GSSLACode"),
                    UrbanRural = root.GetCodeNamePairFromChildElement("UrbanRural"),
                    Federations = root.GetCodeNamePairFromChildElement("Federations"),
                    FederationFlag = root.GetCodeNamePairFromChildElement("FederationFlag"),
                    TelephoneNum = root.GetValueFromChildElement("TelephoneNum"),
                    ContactEmail = root.GetValueFromChildElement("ContactEmail"),
                    Street = root.GetValueFromChildElement("Street"),
                    Locality = root.GetValueFromChildElement("Locality"),
                    Address3 = root.GetValueFromChildElement("Address3"),
                    Town = root.GetValueFromChildElement("Town"),
                    County = root.GetValueFromChildElement("County"),
                    SchoolCapacity = root.GetLongFromChildElement("SchoolCapacity"),
                    NumberOfPupils = root.GetLongFromChildElement("NumberOfPupils"),
                    NumberOfBoys = root.GetLongFromChildElement("NumberOfBoys"),
                    NumberOfGirls = root.GetLongFromChildElement("NumberOfGirls"),
                    ResourcedProvisionCapacity = root.GetLongFromChildElement("ResourcedProvisionCapacity"),
                    ResourcedProvisionOnRoll = root.GetLongFromChildElement("ResourcedProvisionOnRoll"),
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

        private static SoapResponse EnsureSuccessResponseAndExtractResult(IRestResponse response)
        {
            XDocument document;
            try
            {
                var contentParts = ParseResponseContent(response);
                var soapPart = contentParts.SingleOrDefault(p =>
                    p.Headers["Content-Type"] == "application/xop+xml" ||
                    p.Headers["Content-Type"] == "text/xml");
                if (soapPart == null)
                {
                    throw new Exception("Response does not appear to contain any soap content");
                }

                document = XDocument.Parse(Encoding.UTF8.GetString(soapPart.Data));
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

            return new SoapResponse
            {
                Result = body.Elements().First(),
            };
        }

        private static ContentPart[] ParseResponseContent(IRestResponse response)
        {
            if (response.ContentType.StartsWith("Multipart/Related"))
            {
                return null;
            }
            else
            {
                return new[]
                {
                    new ContentPart
                    {
                        Headers = new Dictionary<string, string>
                        {
                            {"Content-Type", response.ContentType}
                        },
                        Data = response.RawBytes,
                    },
                };
            }
        }


        private class SoapResponse
        {
            public XElement Result { get; set; }
            public object[] Attachments { get; set; }
        }

        private class ContentPart
        {
            public Dictionary<string, string> Headers { get; set; }
            public byte[] Data { get; set; }
        }
    }
}