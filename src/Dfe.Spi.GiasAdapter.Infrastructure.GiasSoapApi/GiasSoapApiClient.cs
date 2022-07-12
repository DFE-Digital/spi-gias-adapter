using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasCsvParsing;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.Requests;
using RestSharp;
using Group = Dfe.Spi.GiasAdapter.Domain.GiasApi.Group;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi
{
    public class GiasSoapApiClient : IGiasApiClient
    {
        private readonly GiasApiConfiguration _configuration;
        private readonly IRestClient _restClient;
        private readonly IGiasSoapMessageBuilder<GetEstablishmentRequest> _getEstablishmentMessageBuilder;
        private readonly IGiasSoapMessageBuilder<GetExtractRequest> _getExtractMessageBuilder;
        private ZipArchive _zip;

        internal GiasSoapApiClient(
            GiasApiConfiguration configuration,
            IRestClient restClient,
            IGiasSoapMessageBuilder<GetEstablishmentRequest> getEstablishmentMessageBuilder,
            IGiasSoapMessageBuilder<GetExtractRequest> getExtractMessageBuilder)
        {
            _configuration = configuration;
            _restClient = restClient;
            _getEstablishmentMessageBuilder = getEstablishmentMessageBuilder;
            _getExtractMessageBuilder = getExtractMessageBuilder;
        }

        public GiasSoapApiClient(GiasApiConfiguration configuration)
            : this(
                configuration,
                new RestClient(configuration.Url),
                new GetEstablishmentMessageBuilder(configuration.Username, configuration.Password),
                new GetExtractMessageBuilder(configuration.Username, configuration.Password))
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

                // NOTE: Mapping appears in order of the properties for the
                //       Establishment entity.
                //       If adding new mapping, please retain order.
                var establishment = new Establishment
                {
                    AdministrativeWard = root.GetCodeNamePairFromChildElement("AdministrativeWard"),
                    AdmissionsPolicy = root.GetCodeNamePairFromChildElement("AdmissionsPolicy"),
                    Boarders = root.GetCodeNamePairFromChildElement("Boarders"),
                    Ccf = root.GetCodeNamePairFromChildElement("CCF"),
                    CloseDate = root.GetDateTimeFromChildElement("CloseDate"),
                    OfstedLastInsp = root.GetDateTimeFromChildElement("OfstedLastInsp"),
                    Diocese = root.GetCodeNamePairFromChildElement("Diocese"),
                    DistrictAdministrative = root.GetCodeNamePairFromChildElement("DistrictAdministrative"),
                    Easting = root.GetLongFromChildElement("Easting"),
                    Ebd = root.GetCodeNamePairFromChildElement("EBD"),
                    EdByOther = root.GetCodeNamePairFromChildElement("EdByOther"),
                    EstablishmentName = root.GetValueFromChildElement("EstablishmentName"),
                    EstablishmentNumber = root.GetLongFromChildElement("EstablishmentNumber"),
                    EstablishmentStatus = root.GetCodeNamePairFromChildElement("EstablishmentStatus"),
                    EstablishmentTypeGroup = root.GetCodeNamePairFromChildElement("EstablishmentTypeGroup"),
                    TypeOfEstablishment = root.GetCodeNamePairFromChildElement("TypeOfEstablishment"),
                    FurtherEducationType = root.GetCodeNamePairFromChildElement("FurtherEducationType"),
                    Gender = root.GetCodeNamePairFromChildElement("Gender"),
                    Gor = root.GetCodeNamePairFromChildElement("GOR"),
                    GsslaCode = root.GetCodeNamePairFromChildElement("GSSLACode"),
                    Inspectorate = root.GetCodeNamePairFromChildElement("Inspectorate"),
                    LA = root.GetCodeNamePairFromChildElement("LA"),
                    LastChangedDate = root.GetDateTimeFromChildElement("LastChangedDate"),
                    Msoa = root.GetCodeNamePairFromChildElement("MSOA"),
                    Northing = root.GetLongFromChildElement("Northing"),
                    NumberOfPupils = root.GetLongFromChildElement("NumberOfPupils"),
                    OfficialSixthForm = root.GetCodeNamePairFromChildElement("OfficialSixthForm"),
                    OfstedRating = root.GetCodeNamePairFromChildElement("OfstedRating"),
                    OpenDate = root.GetDateTimeFromChildElement("OpenDate"),
                    ParliamentaryConstituency = root.GetCodeNamePairFromChildElement("ParliamentaryConstituency"),
                    PercentageFsm = root.GetDecimalFromChildElement("PercentageFSM"),
                    PhaseOfEducation = root.GetCodeNamePairFromChildElement("PhaseOfEducation"),
                    PlacesPru = root.GetLongFromChildElement("PlacesPRU"),
                    Postcode = root.GetValueFromChildElement("Postcode"),
                    PreviousEstablishmentNumber = root.GetLongFromChildElement("PreviousEstablishmentNumber"),
                    ReasonEstablishmentClosed = root.GetCodeNamePairFromChildElement("ReasonEstablishmentClosed"),
                    ReasonEstablishmentOpened = root.GetCodeNamePairFromChildElement("ReasonEstablishmentOpened"),
                    ReligiousEthos = root.GetCodeNamePairFromChildElement("ReligiousEthos"),
                    ResourcedProvisionCapacity = root.GetLongFromChildElement("ResourcedProvisionCapacity"),
                    ResourcedProvisionOnRoll = root.GetLongFromChildElement("ResourcedProvisionOnRoll"),
                    RscRegion = root.GetCodeNamePairFromChildElement("RSCRegion"),
                    SchoolCapacity = root.GetLongFromChildElement("SchoolCapacity"),
                    SchoolWebsite = root.GetValueFromChildElement("SchoolWebsite"),
                    Section41Approved = root.GetCodeNamePairFromChildElement("Section41Approved"),
                    SpecialClasses = root.GetCodeNamePairFromChildElement("SpecialClasses"),
                    StatutoryHighAge = root.GetLongFromChildElement("StatutoryHighAge"),
                    StatutoryLowAge = root.GetLongFromChildElement("StatutoryLowAge"),
                    TeenMoth = root.GetCodeNamePairFromChildElement("TeenMoth"),
                    TeenMothPlaces = root.GetLongFromChildElement("TeenMothPlaces"),
                    TelephoneNum = root.GetValueFromChildElement("TelephoneNum"),
                    Trusts = root.GetElementByLocalName("Trusts")?.GetCodeNamePairFromChildElement("Value"),
                    Ukprn = root.GetLongFromChildElement("UKPRN"),
                    Uprn = root.GetValueFromChildElement("UPRN"),
                    UrbanRural = root.GetCodeNamePairFromChildElement("UrbanRural"),
                    Urn = urn,
                    Lsoa = root.GetCodeNamePairFromChildElement("LSOA"),
                    DateOfLastInspectionVisit = root.GetDateTimeFromChildElement("DateOfLastInspectionVisit"),
                    InspectorateReport = root.GetValueFromChildElement("InspectorateReport"),
                    ContactEmail = root.GetValueFromChildElement("ContactEmail"),
                    Street = root.GetValueFromChildElement("Street"),
                    Locality = root.GetValueFromChildElement("Locality"),
                    Address3 = root.GetValueFromChildElement("Address3"),
                    Town = root.GetValueFromChildElement("Town"),
                    County = root.GetCodeNamePairFromChildElement("County"),
                    Federations = root.GetCodeNamePairFromChildElement("Federations"),
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

        public async Task<Establishment[]> DownloadEstablishmentsAsync(CancellationToken cancellationToken)
        {
            if (_zip == null)
            {
                await AcquireExtract(cancellationToken);
            }

            var zipEntry = _zip.Entries.SingleOrDefault(e => e.Name == _configuration.ExtractEstablishmentsFileName);
            if (zipEntry == null)
            {
                throw new Exception($"Extract does not contain entry for {_configuration.ExtractEstablishmentsFileName}");
            }

            using (var stream = zipEntry.Open())
            using (var reader = new StreamReader(stream))
            using (var parser = new EstablishmentFileParser(reader))
            {
                var establishments = parser.GetRecords();
                return establishments;
            }
        }

        public async Task<Group[]> DownloadGroupsAsync(CancellationToken cancellationToken)
        {
            if (_zip == null)
            {
                await AcquireExtract(cancellationToken);
            }

            var zipEntry = _zip.Entries.SingleOrDefault(e => e.Name == _configuration.ExtractGroupsFileName);
            if (zipEntry == null)
            {
                throw new Exception($"Extract does not contain entry for {_configuration.ExtractGroupsFileName}");
            }

            using (var stream = zipEntry.Open())
            using (var reader = new StreamReader(stream))
            using (var parser = new GroupFileParser(reader))
            {
                var groups = parser.GetRecords();
                return groups;
            }
        }

        public async Task<GroupLink[]> DownloadGroupLinksAsync(CancellationToken cancellationToken)
        {
            if (_zip == null)
            {
                await AcquireExtract(cancellationToken);
            }

            var zipEntry = _zip.Entries.SingleOrDefault(e => e.Name == _configuration.ExtractGroupLinksFileName);
            if (zipEntry == null)
            {
                throw new Exception($"Extract does not contain entry for {_configuration.ExtractGroupLinksFileName}");
            }

            using (var stream = zipEntry.Open())
            using (var reader = new StreamReader(stream))
            using (var parser = new GroupLinkFileParser(reader))
            {
                var groupLinks = parser.GetRecords();
                return groupLinks;
            }
        }


        private async Task AcquireExtract(CancellationToken cancellationToken)
        {
            var message = _getExtractMessageBuilder.Build(new GetExtractRequest
            {
                ExtractId = _configuration.ExtractId,
            });

            var request = new RestRequest(Method.POST);
            request.AddParameter("text/xml", message, ParameterType.RequestBody);
            request.AddHeader("SOAPAction", "http://ws.edubase.texunatech.com/GetExtract");

            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            var soapResponse = EnsureSuccessResponseAndExtractResult(response);
            var zipAttachment = soapResponse.Attachments.FirstOrDefault();
            if (zipAttachment == null)
            {
                throw new Exception("Missing zip attachment");
            }
            
            _zip = new ZipArchive(new MemoryStream(zipAttachment.Data));
        }

        private static SoapResponse EnsureSuccessResponseAndExtractResult(IRestResponse response)
        {
            ContentPart[] contentParts;
            ContentPart soapPart;
            XDocument document;
            try
            {
                contentParts = ParseResponseContent(response);
                soapPart = contentParts.SingleOrDefault(p =>
                    p.Headers["Content-Type"].StartsWith("application/xop+xml") ||
                    p.Headers["Content-Type"].StartsWith("text/xml"));
                if (soapPart == null)
                {
                    throw new Exception("Response does not appear to contain any soap content");
                }

                document = XDocument.Parse(Encoding.UTF8.GetString(soapPart.Data));
            }
            catch (Exception ex)
            {
                throw new GiasSoapApiException(
                    $"Error deserializing SOAP response: {ex.Message}", ex);
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
                Attachments = contentParts.Where(p => p != soapPart).ToArray(),
            };
        }

        private static ContentPart[] ParseResponseContent(IRestResponse response)
        {
            if (response.ContentType.StartsWith("Multipart/Related"))
            {
                return ReadMultipart(response);
            }

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

        private static ContentPart[] ReadMultipart(IRestResponse response)
        {
            // Get boundary
            var boundaryMatch =
                Regex.Match(response.ContentType, "boundary=\"([a-z0-9\\-=_\\.]{1,})\"", RegexOptions.IgnoreCase);
            if (!boundaryMatch.Success)
            {
                throw new Exception("Multipart response does not have boundary specified");
            }

            // Split content by boundary
            var boundary = Encoding.UTF8.GetBytes($"--{boundaryMatch.Groups[1].Value}");
            var index = 0;
            int lastIndex = -1;
            var parts = new List<byte[]>();
            var data = response.RawBytes;
            while ((index = IndexOf(data, boundary, index)) >= 0)
            {
                if (lastIndex > -1)
                {
                    var start = lastIndex + boundary.Length + 2;
                    var length = index - start - 2;
                    var partBuffer = new byte[length];
                    Array.Copy(data, start, partBuffer, 0, length);
                    parts.Add(partBuffer);
                }

                lastIndex = index;
                index += 1;
            }

            // Convert to content parts
            var splitter = Encoding.UTF8.GetBytes("\r\n\r\n");
            var contentParts = new ContentPart[parts.Count];
            for (var i = 0; i < parts.Count; i++)
            {
                var headerContentSplit = IndexOf(parts[i], splitter);

                var headersBuffer = new byte[headerContentSplit];
                Array.Copy(parts[i], headersBuffer, headersBuffer.Length);
                var headers = Encoding.UTF8.GetString(headersBuffer)
                    .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Split(':'))
                    .ToDictionary(x => x[0].Trim(), x => x[1].Trim());

                var bodyBuffer = new byte[parts[i].Length - headerContentSplit - splitter.Length];
                Array.Copy(parts[i], headerContentSplit + splitter.Length, bodyBuffer, 0, bodyBuffer.Length);

                contentParts[i] = new ContentPart
                {
                    Headers = headers,
                    Data = bodyBuffer,
                };
            }

            // And breathe
            return contentParts;
        }

        private static int IndexOf(byte[] buffer, byte[] value, int startIndex = 0)
        {
            var index = startIndex;
            while (index + value.Length < buffer.Length)
            {
                var isMatch = true;
                for (var i = 0; i < value.Length && isMatch; i++)
                {
                    isMatch = buffer[index + i] == value[i];
                }

                if (isMatch)
                {
                    return index;
                }

                index += 1;
            }

            return -1;
        }


        private class SoapResponse
        {
            public XElement Result { get; set; }
            public ContentPart[] Attachments { get; set; }
        }

        private class ContentPart
        {
            public Dictionary<string, string> Headers { get; set; }
            public byte[] Data { get; set; }
        }
    }
}