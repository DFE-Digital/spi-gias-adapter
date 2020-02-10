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
                Map(x => x.EstablishmentTypeGroup).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentTypeGroup"));
                Map(x => x.TypeOfEstablishment).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "TypeOfEstablishment"));
                Map(x => x.EstablishmentStatus).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentStatus"));
                Map(x => x.OpenDate).Name("OpenDate");
                Map(x => x.CloseDate).Name("CloseDate");
                Map(x => x.LA).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "LA"));
                Map(x => x.Postcode).Name("Postcode");
                Map(x => x.EstablishmentName).Name("EstablishmentName");
                Map(x => x.Urn).Name("URN");
                Map(x => x.Ukprn).Name("UKPRN");
                Map(x => x.Uprn).Name("UPRN");
                Map(x => x.Trusts).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Trusts"));
                Map(x => x.EstablishmentNumber).Name("EstablishmentNumber");
                Map(x => x.PreviousEstablishmentNumber).Name("PreviousEstablishmentNumber");

                Map(x => x.Boarders).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Boarders"));
                Map(x => x.StatutoryLowAge).Name("StatutoryLowAge");
                Map(x => x.StatutoryHighAge).Name("StatutoryHighAge");
                Map(x => x.SchoolWebsite).Name("SchoolWebsite");
                Map(x => x.Gender).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Gender"));
                Map(x => x.PercentageFsm).Name("PercentageFSM");
                Map(x => x.OfstedLastInsp).Name("OfstedLastInsp");
                Map(x => x.LastChangedDate).Name("LastChangedDate");
                Map(x => x.DateOfLastInspectionVisit).Name("DateOfLastInspectionVisit");
                Map(x => x.OfstedRatingName).Name("OfstedRating (name)");
                Map(x => x.AdmissionsPolicy).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "AdmissionsPolicy"));
                Map(x => x.InspectorateNameName).Name("InspectorateName (name)");
                Map(x => x.InspectorateReport).Name("InspectorateReport");
                Map(x => x.TeenMothName).Name("TeenMoth (name)");
                Map(x => x.TeenMothPlaces).Name("TeenMothPlaces");
                Map(x => x.ReasonEstablishmentOpened).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReasonEstablishmentOpened"));
                Map(x => x.ReasonEstablishmentClosed).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReasonEstablishmentClosed"));
                Map(x => x.PhaseOfEducation).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "PhaseOfEducation"));
                Map(x => x.FurtherEducationTypeName).Name("FurtherEducationType (name)");
                Map(x => x.OfficialSixthForm).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "OfficialSixthForm"));
                // TODO: Looks like the code parts of CodeNamePairs can be
                //       strings!
                // Map(x => x.Diocese).ConvertUsing(
                //    x => this.BuildCodeNamePair(x, "Diocese"));
                Map(x => x.PreviousLA).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "PreviousLA"));
            }

            private CodeNamePair BuildCodeNamePair(
                IReaderRow readerRow,
                string fieldName)
            {
                CodeNamePair toReturn = new CodeNamePair()
                {
                    Code = readerRow.GetField<string>($"{fieldName} (code)"),
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