using System.IO;
using CsvHelper;
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
                Map(x => x.Name).Name("EstablishmentName");

                Map(x => x.Boarders).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Boarders"));

                Map(x => x.StatutoryLowAge).Name("StatutoryLowAge");
                Map(x => x.StatutoryHighAge).Name("StatutoryHighAge");
                Map(x => x.SchoolWebsite).Name("SchoolWebsite");

                Map(x => x.Gender).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Gender"));

                Map(x => x.PercentageFsm).Name("PercentageFSM");
                Map(x => x.OpenDate).Name("OpenDate");
                Map(x => x.CloseDate).Name("CloseDate");

                Map(x => x.EstablishmentStatus).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentStatus"));

                // End, in-line with spreadsheet.
                Map(x => x.Urn).Name("URN");
                Map(x => x.Ukprn).Name("UKPRN");
                Map(x => x.Uprn).Name("UPRN");
                Map(x => x.AcademyTrustCode).Name("Trusts (code)");
                Map(x => x.LocalAuthorityCode).Name("LA (code)");
                Map(x => x.EstablishmentNumber).Name("EstablishmentNumber");
                Map(x => x.PreviousEstablishmentNumber).Name("PreviousEstablishmentNumber");
            }

            private CodeNamePair BuildCodeNamePair(
                IReaderRow readerRow,
                string fieldName)
            {
                CodeNamePair toReturn = new CodeNamePair()
                {
                    Code = readerRow.GetField<int>($"{fieldName} (code)"),
                    DisplayName = readerRow.GetField<string>($"{fieldName} (name)"),
                };

                return toReturn;
            }
        }

        public EstablishmentFileParser(StreamReader reader)
            : base(reader, new EstablishmentCsvMapping())
        {
        }
    }
}