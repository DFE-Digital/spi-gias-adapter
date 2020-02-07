using System;

namespace Dfe.Spi.GiasAdapter.Domain.GiasApi
{
    public class Establishment
    {
        public long Urn { get; set; }
        public string Name { get; set; }
        public long? Ukprn { get; set; }
        public string Uprn { get; set; }
        public string CompaniesHouseNumber { get; set; }
        public string CharitiesCommissionNumber { get; set; }
        public string AcademyTrustCode { get; set; }
        public string LocalAuthorityCode { get; set; }
        public string EstablishmentNumber { get; set; }
        public string PreviousEstablishmentNumber { get; set; }
        public string Postcode { get; set; }
        public CodeNamePair EstablishmentStatus { get; set; }
        public CodeNamePair EstablishmentTypeGroup { get; set; }
        public CodeNamePair TypeOfEstablishment { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
    }
}