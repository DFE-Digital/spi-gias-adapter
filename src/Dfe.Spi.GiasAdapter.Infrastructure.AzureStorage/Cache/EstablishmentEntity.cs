using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    internal class EstablishmentEntity : TableEntity
    {
        public long Urn { get; set; }
        public string Name { get; set; }
        public long? Ukprn { get; set; }
        public string CompaniesHouseNumber { get; set; }
        public string CharitiesCommissionNumber { get; set; }
        public string AcademyTrustCode { get; set; }
        public string LocalAuthorityCode { get; set; }
        public string EstablishmentNumber { get; set; }
        public string PreviousEstablishmentNumber { get; set; }
        public string Postcode { get; set; }
    }
}