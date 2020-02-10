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
        public CodeNamePair La { get; set; }
        public string Postcode { get; set; }
        public string EstablishmentName { get; set; }
        public long Urn { get; set; }
        public long? Ukprn { get; set; }
        public string Uprn { get; set; }
        public CodeNamePair Trusts { get; set; }
        public int? EstablishmentNumber { get; set; }
        public int? PreviousEstablishmentNumber { get; set; }

        public CodeNamePair Boarders { get; set; }
        public string BoardersName { get; set; }
        public int? StatutoryLowAge { get; set; }
        public int? StatutoryHighAge { get; set; }
        public string SchoolWebsite { get; set; }
        public CodeNamePair Gender { get; set; }
        public decimal? PercentageFsm { get; set; }
        public DateTime? OfstedLastInsp { get; set; }
        public DateTime? LastChangedDate { get; set; }
        public DateTime? DateOfLastInspectionVisit { get; set; }
        public string OfstedRatingName { get; set; }
        public string CompaniesHouseNumber { get; set; }
        public CodeNamePair AdmissionsPolicy { get; set; }
        public string InspectorateNameName { get; set; }
        public string InspectorateReport { get; set; }
        public string TeenMothName { get; set; }
        public int? TeenMothPlaces { get; set; }

        // Not being populated - but leaving on the model for now, as not
        // to break things.
        public string CharitiesCommissionNumber { get; set; }
    }
}