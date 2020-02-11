using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload.CsvParsing;
using NUnit.Framework;
using System.IO;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload.UnitTests
{
    public class WhenParsingEstablishmentsCsv
    {
        [Test]
        [SetCulture("en-GB")]
        public void ThenItShouldParseWithSuccess()
        {
            FileInfo sampleFile = new FileInfo(
                "Samples/edubasealldata20200210.csv");

            FileStream fileStream = sampleFile.OpenRead();
            StreamReader streamReader = new StreamReader(fileStream);

            EstablishmentFileParser establishmentFileParser =
                new EstablishmentFileParser(streamReader);

            Establishment[] establishments =
                establishmentFileParser.GetRecords();

            Assert.AreEqual(establishments.Length, 48354);
        }
    }
}
