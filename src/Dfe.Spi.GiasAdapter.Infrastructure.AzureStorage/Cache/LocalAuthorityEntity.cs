using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class LocalAuthorityEntity : TableEntity
    {
        public string LocalAuthority
        {
            get;
            set;
        }
    }
}