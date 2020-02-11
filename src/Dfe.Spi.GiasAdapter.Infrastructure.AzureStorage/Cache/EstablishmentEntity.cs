using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    internal class EstablishmentEntity : TableEntity
    {
        public string Establishment
        {
            get;
            set;
        }
    }
}