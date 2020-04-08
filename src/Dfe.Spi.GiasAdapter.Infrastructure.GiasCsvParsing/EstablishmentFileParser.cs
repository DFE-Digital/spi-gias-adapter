using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasCsvParsing
{
    public class EstablishmentFileParser : CsvFileParser<Establishment>
    {
        private class EstablishmentCsvMapping : ClassMap<Establishment>
        {
            public EstablishmentCsvMapping()
            {
                var dateTimeConverter = new DateTimeConverter();

                // NOTE: Mapping appears in order of the properties for the
                //       Establishment entity.
                //       If adding new mapping, please retain order.
                Map(x => x.AdministrativeWard).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "AdministrativeWard"));
                Map(x => x.AdmissionsPolicy).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "AdmissionsPolicy"));
                Map(x => x.Boarders).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Boarders"));
                Map(x => x.Ccf).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "CCF"));
                Map(x => x.CloseDate).Name("CloseDate").TypeConverter(dateTimeConverter);
                Map(x => x.OfstedLastInsp).Name("OfstedLastInsp").TypeConverter(dateTimeConverter);
                Map(x => x.Diocese).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Diocese"));
                Map(x => x.DistrictAdministrative).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "DistrictAdministrative"));
                Map(x => x.Easting).Name("Easting");
                Map(x => x.Ebd).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EBD"));
                Map(x => x.EdByOther).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EdByOther"));
                Map(x => x.EstablishmentName).Name("EstablishmentName");
                Map(x => x.EstablishmentNumber).Name("EstablishmentNumber");
                Map(x => x.EstablishmentStatus).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentStatus"));
                Map(x => x.EstablishmentTypeGroup).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentTypeGroup"));
                Map(x => x.TypeOfEstablishment).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "TypeOfEstablishment"));
                Map(x => x.FurtherEducationType).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "FurtherEducationType"));
                Map(x => x.Gender).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Gender"));
                Map(x => x.Gor).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "GOR"));
                Map(x => x.GsslaCode).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "GSSLACode"));
                Map(x => x.Inspectorate).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Inspectorate"));
                Map(x => x.LA).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "LA"));
                Map(x => x.LastChangedDate).Name("LastChangedDate").TypeConverter(dateTimeConverter);
                Map(x => x.Msoa).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "MSOA"));
                Map(x => x.Northing).Name("Northing");
                Map(x => x.NumberOfPupils).Name("NumberOfPupils");
                Map(x => x.OfficialSixthForm).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "OfficialSixthForm"));
                Map(x => x.OfstedRating).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "OfstedRating"));
                Map(x => x.OpenDate).Name("OpenDate").TypeConverter(dateTimeConverter);
                Map(x => x.ParliamentaryConstituency)
                    .ConvertUsing(x => this.BuildCodeNamePair(x, "ParliamentaryConstituency"));
                Map(x => x.PercentageFsm).Name("PercentageFSM");
                Map(x => x.PhaseOfEducation).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "PhaseOfEducation"));
                Map(x => x.PlacesPru).Name("PlacesPRU");
                Map(x => x.Postcode).Name("Postcode");
                Map(x => x.PreviousEstablishmentNumber).Name("PreviousEstablishmentNumber");
                Map(x => x.ReasonEstablishmentClosed).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReasonEstablishmentClosed"));
                Map(x => x.ReasonEstablishmentOpened).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReasonEstablishmentOpened"));
                Map(x => x.ReligiousEthos).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReligiousEthos"));
                Map(x => x.ResourcedProvisionCapacity).Name("ResourcedProvisionCapacity");
                Map(x => x.ResourcedProvisionOnRoll).Name("ResourcedProvisionOnRoll");
                Map(x => x.RscRegion).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "RSCRegion"));
                Map(x => x.SchoolCapacity).Name("SchoolCapacity");
                Map(x => x.SchoolWebsite).Name("SchoolWebsite");
                Map(x => x.Section41Approved).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Section41Approved"));
                Map(x => x.SpecialClasses).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "SpecialClasses"));
                Map(x => x.StatutoryHighAge).Name("StatutoryHighAge");
                Map(x => x.StatutoryLowAge).Name("StatutoryLowAge");
                Map(x => x.TeenMoth).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "TeenMoth"));
                Map(x => x.TeenMothPlaces).Name("TeenMothPlaces");
                Map(x => x.TelephoneNum).Name("TelephoneNum");
                Map(x => x.Trusts).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Trusts"));
                Map(x => x.Ukprn).Name("UKPRN");
                Map(x => x.Uprn).Name("UPRN");
                Map(x => x.UrbanRural).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "UrbanRural"));
                Map(x => x.Urn).Name("URN");
                Map(x => x.Lsoa).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "LSOA"));
                Map(x => x.DateOfLastInspectionVisit).Name("DateOfLastInspectionVisit").TypeConverter(dateTimeConverter);
                Map(x => x.InspectorateReport).Name("InspectorateReport");
                Map(x => x.ContactEmail).Name("ContactEmail");
                Map(x => x.Street).Name("Street");
                Map(x => x.Locality).Name("Locality");
                Map(x => x.Address3).Name("Address3");
                Map(x => x.Town).Name("Town");
                Map(x => x.County).Name("County");
                Map(x => x.Federations).Name("Federations");
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