using Microsoft.Azure.Cosmos.Table;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class GroupEntity : TableEntity
    {
        public string Group
        {
            get;
            set;
        }
    }
}