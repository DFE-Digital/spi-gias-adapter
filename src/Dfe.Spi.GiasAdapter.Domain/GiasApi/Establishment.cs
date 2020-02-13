using System;

namespace Dfe.Spi.GiasAdapter.Domain.GiasApi
{
    public class Establishment
    {
        public CodeNamePair EstablishmentTypeGroup { get; set; }
        public CodeNamePair TypeOfEstablishment { get; set; }
        public CodeNamePair EstablishmentStatus { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public CodeNamePair LA { get; set; }
        public string Postcode { get; set; }
        public string EstablishmentName { get; set; }
        public long Urn { get; set; }
        public long? Ukprn { get; set; }
        public string Uprn { get; set; }
        public CodeNamePair Trusts { get; set; }
        public long? EstablishmentNumber { get; set; }
        public long? PreviousEstablishmentNumber { get; set; }

        public CodeNamePair Boarders { get; set; }
        public long? StatutoryLowAge { get; set; }
        public long? StatutoryHighAge { get; set; }
        public string SchoolWebsite { get; set; }
        public CodeNamePair Gender { get; set; }
        public decimal? PercentageFsm { get; set; }
        public DateTime? OfstedLastInsp { get; set; }
        public DateTime? LastChangedDate { get; set; }
        public DateTime? DateOfLastInspectionVisit { get; set; }
        public CodeNamePair OfstedRating { get; set; }
        public CodeNamePair AdmissionsPolicy { get; set; }
        public CodeNamePair InspectorateName { get; set; }
        public string InspectorateReport { get; set; }
        public CodeNamePair TeenMoth { get; set; }
        public long? TeenMothPlaces { get; set; }
        public CodeNamePair ReasonEstablishmentOpened { get; set; }
        public CodeNamePair ReasonEstablishmentClosed { get; set; }
        public CodeNamePair PhaseOfEducation { get; set; }
        public CodeNamePair FurtherEducationType { get; set; }
        public CodeNamePair OfficialSixthForm { get; set; }
        public CodeNamePair Diocese { get; set; }
        public CodeNamePair PreviousLA { get; set; }
        public CodeNamePair DistrictAdministrative { get; set; }
        public CodeNamePair AdministrativeWard { get; set; }
        public CodeNamePair Gor { get; set; }
        public CodeNamePair Msoa { get; set; }
        public CodeNamePair Lsoa { get; set; }
        public CodeNamePair RscRegion { get; set; }
        public CodeNamePair Section41Approved { get; set; }
        public long? Easting { get; set; }
        public long? Northing { get; set; }
        public CodeNamePair ParliamentaryConstituency { get; set; }
        public CodeNamePair GsslaCode { get; set; }
        public CodeNamePair UrbanRural { get; set; }
        public CodeNamePair Federations { get; set; }
        public CodeNamePair FederationFlag { get; set; }
        public string TelephoneNum { get; set; }
        public string ContactEmail { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public long? SchoolCapacity { get; set; }
        public long? NumberOfPupils { get; set; }
        public long? NumberOfBoys { get; set; }
        public long? NumberOfGirls { get; set; }
        public long? ResourcedProvisionCapacity { get; set; }
        public long? ResourcedProvisionOnRoll { get; set; }

        // Not being populated - but leaving on the model for now, as not
        // to break things.
        public string CharitiesCommissionNumber { get; set; }
        public string CompaniesHouseNumber { get; set; }
    }
}