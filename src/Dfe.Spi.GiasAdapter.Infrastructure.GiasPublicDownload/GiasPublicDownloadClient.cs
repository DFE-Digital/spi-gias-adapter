using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasCsvParsing;
using HtmlAgilityPack;
using RestSharp;
using Group = Dfe.Spi.GiasAdapter.Domain.GiasApi.Group;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload
{
    public class GiasPublicDownloadClient : IGiasApiClient
    {
        private readonly IRestClient _restClient;
        private readonly ILoggerWrapper _logger;

        public GiasPublicDownloadClient(IRestClient restClient, ILoggerWrapper logger)
        {
            _restClient = restClient;
            _logger = logger;
        }

        public Task<Establishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Establishment[]> DownloadEstablishmentsAsync(CancellationToken cancellationToken)
        {
            var downloadLinks = await GetAvailableDownloadLinksAsync(cancellationToken);
            var downloadLinksNames = downloadLinks.Select(x => x.Title).Aggregate((x, y) => $"{x}, {y}");
            _logger.Debug($"Found {downloadLinks.Length} download links - {downloadLinksNames}");
            
            var establishmentLink =
                downloadLinks.SingleOrDefault(l =>
                    l.Title.Equals("Establishment fields", StringComparison.CurrentCultureIgnoreCase));

            if (establishmentLink == null)
            {
                _logger.Debug($"Failed to find link with text 'Establishment fields'");
                return new Establishment[0];
            }

            var csv = await DownloadCsvAsync(establishmentLink.Url, cancellationToken);
            _logger.Debug($"Downloaded csv of {csv.Length} bytes");
            
            using (var stream = new MemoryStream(csv))
            using (var reader = new StreamReader(stream))
            using (var parser = new EstablishmentFileParser(reader))
            {
                var establishments = parser.GetRecords();
                return establishments;
            }
        }

        public Task<Group[]> DownloadGroupsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<GroupLink[]> DownloadGroupLinksAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        private async Task<DownloadLink[]> GetAvailableDownloadLinksAsync(CancellationToken cancellationToken)
        {
            const string downloadUrl = "https://get-information-schools.service.gov.uk/Downloads";

            var request = new RestRequest(downloadUrl, Method.GET);
            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            if (!response.IsSuccessful)
            {
                throw new Exception($"Error getting list of download links from {downloadUrl}. " +
                                    $"Status {(int) response.StatusCode}. Details {response.Content}");
            }

            var page = new HtmlDocument();
            page.LoadHtml(response.Content);

            var listItems = page.DocumentNode.Descendants("li").Where(e => e.HasClass("download-list-item")).ToArray();
            var anchors = listItems.Select(li => li.Descendants("a").SingleOrDefault()).ToArray();
            return anchors.Select(a =>
            {
                var fullTitle = a.InnerText.Trim();

                var match = Regex.Match(fullTitle,
                    "^([a-z\\s]{1,})\\s{0,1}\\.{0,1}csv\\,\\s[0-9]{1,}\\.[0-9]{1,2}\\s[MK]B\\sOpens\\sin\\snew\\swindow$",
                    RegexOptions.IgnoreCase);
                var title = match.Success
                    ? match.Groups[1].Value.Trim()
                    : fullTitle;

                return new DownloadLink
                {
                    Title = title,
                    FullTitle = fullTitle,
                    Url = a.GetAttributeValue("href", string.Empty),
                };
            }).ToArray();
        }

        private async Task<byte[]> DownloadCsvAsync(string url, CancellationToken cancellationToken)
        {
            var request = new RestRequest(url, Method.GET);
            var response = await _restClient.ExecuteTaskAsync(request, cancellationToken);
            if (!response.IsSuccessful)
            {
                throw new Exception($"Error downloading csv from {url}. " +
                                    $"Status {(int) response.StatusCode}. Details {response.Content}");
            }

            return response.RawBytes;
        }

        private class DownloadLink
        {
            public string Title { get; set; }
            public string FullTitle { get; set; }
            public string Url { get; set; }
        }
    }
}