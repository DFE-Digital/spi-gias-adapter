using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    internal class EstablishmentEntity : TableEntity
    {
        public long Urn { get; set; }
        public string Name { get; set; }
    }
}