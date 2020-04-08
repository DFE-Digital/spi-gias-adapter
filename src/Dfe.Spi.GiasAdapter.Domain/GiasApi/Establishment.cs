using System;

namespace Dfe.Spi.GiasAdapter.Domain.GiasApi
{
    /// <summary>
    /// NOTE: These are ordered the same according to the mapping in
    ///       EstablishmentMapper.
    ///       Which are in turn, ordered according to the SPI models and
    ///       spreadsheet.
    ///       If adding to this list, please retain the order!
    /// </summary>
    public class Establishment
    {
        public CodeNamePair AdministrativeWard { get; set; }
        public CodeNamePair AdmissionsPolicy { get; set; }
        public CodeNamePair Boarders { get; set; }
        public CodeNamePair Ccf { get; set; }
        public DateTime? CloseDate { get; set; }
        public DateTime? OfstedLastInsp { get; set; }
        public CodeNamePair Diocese { get; set; }
        public CodeNamePair DistrictAdministrative { get; set; }
        public long? Easting { get; set; }
        public CodeNamePair Ebd { get; set; }
        public CodeNamePair EdByOther { get; set; }
        public string EstablishmentName { get; set; }
        public long? EstablishmentNumber { get; set; }
        public CodeNamePair EstablishmentStatus { get; set; }
        public CodeNamePair EstablishmentTypeGroup { get; set; }
        public CodeNamePair TypeOfEstablishment { get; set; }
        public CodeNamePair FurtherEducationType { get; set; }
        public CodeNamePair Gender { get; set; }
        public CodeNamePair Gor { get; set; }
        public CodeNamePair GsslaCode { get; set; }
        public CodeNamePair Inspectorate { get; set; }
        public CodeNamePair LA { get; set; }
        public DateTime? LastChangedDate { get; set; }
        public CodeNamePair Msoa { get; set; }
        public long? Northing { get; set; }
        public long? NumberOfPupils { get; set; }
        public CodeNamePair OfficialSixthForm { get; set; }
        public CodeNamePair OfstedRating { get; set; }
        public DateTime? OpenDate { get; set; }
        public CodeNamePair ParliamentaryConstituency { get; set; }
        public decimal? PercentageFsm { get; set; }
        public CodeNamePair PhaseOfEducation { get; set; }
        public long? PlacesPru { get; set; }
        public string Postcode { get; set; }
        public long? PreviousEstablishmentNumber { get; set; }
        public CodeNamePair ReasonEstablishmentClosed { get; set; }
        public CodeNamePair ReasonEstablishmentOpened { get; set; }
        public CodeNamePair ReligiousEthos { get; set; }
        public long? ResourcedProvisionCapacity { get; set; }
        public long? ResourcedProvisionOnRoll { get; set; }
        public CodeNamePair RscRegion { get; set; }
        public long? SchoolCapacity { get; set; }
        public string SchoolWebsite { get; set; }
        public CodeNamePair Section41Approved { get; set; }
        public CodeNamePair SpecialClasses { get; set; }
        public long? StatutoryHighAge { get; set; }
        public long? StatutoryLowAge { get; set; }
        public CodeNamePair TeenMoth { get; set; }
        public long? TeenMothPlaces { get; set; }
        public string TelephoneNum { get; set; }
        public CodeNamePair Trusts { get; set; }
        public long? Ukprn { get; set; }
        public string Uprn { get; set; }
        public CodeNamePair UrbanRural { get; set; }
        public long Urn { get; set; }

        // Not being populated - appears to originate from a different
        // process - leaving on model, in order.
        public string CharitiesCommissionNumber { get; set; }
        public string CompaniesHouseNumber { get; set; }

        public CodeNamePair Lsoa { get; set; }
        public DateTime? DateOfLastInspectionVisit { get; set; }
        public string InspectorateReport { get; set; }
        public string ContactEmail { get; set; }
        public string Street { get; set; }
        public string Locality { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }

        // Used by Management Group syncing, so needs to remain.
        // Not mapped up to spi-models.
        public CodeNamePair Federations { get; set; }
    }
}