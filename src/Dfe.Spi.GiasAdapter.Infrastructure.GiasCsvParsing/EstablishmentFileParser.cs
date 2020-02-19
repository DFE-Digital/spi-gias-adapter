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
                
                Map(x => x.EstablishmentTypeGroup).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentTypeGroup"));
                Map(x => x.TypeOfEstablishment).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "TypeOfEstablishment"));
                Map(x => x.EstablishmentStatus).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "EstablishmentStatus"));
                Map(x => x.OpenDate).Name("OpenDate").TypeConverter(dateTimeConverter);
                Map(x => x.CloseDate).Name("CloseDate").TypeConverter(dateTimeConverter);
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
                Map(x => x.OfstedLastInsp).Name("OfstedLastInsp").TypeConverter(dateTimeConverter);
                Map(x => x.LastChangedDate).Name("LastChangedDate").TypeConverter(dateTimeConverter);
                Map(x => x.DateOfLastInspectionVisit).Name("DateOfLastInspectionVisit").TypeConverter(dateTimeConverter);
                Map(x => x.OfstedRating).ConvertUsing(
                    x=> this.BuildCodeNamePair(x, "OfstedRating"));
                Map(x => x.AdmissionsPolicy).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "AdmissionsPolicy"));
                Map(x => x.InspectorateName).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "InspectorateName"));
                Map(x => x.InspectorateReport).Name("InspectorateReport");
                Map(x => x.TeenMoth).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "TeenMoth"));
                Map(x => x.TeenMothPlaces).Name("TeenMothPlaces");
                Map(x => x.ReasonEstablishmentOpened).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReasonEstablishmentOpened"));
                Map(x => x.ReasonEstablishmentClosed).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "ReasonEstablishmentClosed"));
                Map(x => x.PhaseOfEducation).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "PhaseOfEducation"));
                Map(x => x.FurtherEducationType).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "FurtherEducationType"));
                Map(x => x.OfficialSixthForm).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "OfficialSixthForm"));
                Map(x => x.Diocese).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Diocese"));
                Map(x => x.PreviousLA).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "PreviousLA"));
                Map(x => x.DistrictAdministrative).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "DistrictAdministrative"));
                Map(x => x.AdministrativeWard).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "AdministrativeWard"));
                Map(x => x.Gor).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "GOR"));
                Map(x => x.Msoa).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "MSOA"));
                Map(x => x.Lsoa)
                    .ConvertUsing(x => this.BuildCodeNamePair(x, "LSOA"));
                Map(x => x.RscRegion).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "RSCRegion"));
                Map(x => x.Section41Approved).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Section41Approved"));
                Map(x => x.Easting).Name("Easting");
                Map(x => x.Northing).Name("Northing");
                Map(x => x.ParliamentaryConstituency)
                    .ConvertUsing(x => this.BuildCodeNamePair(x, "ParliamentaryConstituency"));
                Map(x => x.GsslaCode).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "GSSLACode"));
                Map(x => x.UrbanRural).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "UrbanRural"));
                Map(x => x.Federations).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "Federations"));
                Map(x => x.FederationFlag).ConvertUsing(
                    x => this.BuildCodeNamePair(x, "FederationFlag"));
                Map(x => x.TelephoneNum).Name("TelephoneNum");
                Map(x => x.ContactEmail).Name("ContactEmail");
                Map(x => x.Street).Name("Street");
                Map(x => x.Locality).Name("Locality");
                Map(x => x.Address3).Name("Address3");
                Map(x => x.Town).Name("Town");
                Map(x => x.County).Name("County");
                Map(x => x.SchoolCapacity).Name("SchoolCapacity");
                Map(x => x.NumberOfPupils).Name("NumberOfPupils");
                Map(x => x.NumberOfBoys).Name("NumberOfBoys");
                Map(x => x.NumberOfGirls).Name("NumberOfGirls");
                Map(x => x.ResourcedProvisionCapacity).Name("ResourcedProvisionCapacity");
                Map(x => x.ResourcedProvisionOnRoll).Name("ResourcedProvisionOnRoll");
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