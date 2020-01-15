using System.IO;
using CsvHelper.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasPublicDownload.CsvParsing
{
    internal class EstablishmentFileParser : CsvFileParser<Establishment>
    {
        private class EstablishmentCsvMapping : ClassMap<Establishment>
        {
            public EstablishmentCsvMapping()
            {
                Map(x => x.Urn).Name("URN");
                Map(x => x.Name).Name("EstablishmentName");
                Map(x => x.Ukprn).Name("UKPRN");
            }   
        }
        
        public EstablishmentFileParser(StreamReader reader)
            : base(reader, new EstablishmentCsvMapping())
        {
        }
    }
}