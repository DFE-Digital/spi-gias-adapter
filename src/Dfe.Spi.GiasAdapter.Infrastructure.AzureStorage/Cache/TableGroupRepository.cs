using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class TableGroupRepository : TableCacheRepository<Group, GroupEntity>,  IGroupRepository
    {
        public TableGroupRepository(CacheConfiguration configuration, ILoggerWrapper logger) 
            : base(configuration.TableStorageConnectionString, configuration.GroupTableName, logger)
        {
        }
        
        
        public async Task StoreInStagingAsync(Group[] groups, CancellationToken cancellationToken)
        {
            await InsertOrUpdateStagingAsync(groups, cancellationToken);
        }

        
        protected override GroupEntity ModelToEntity(Group model)
        {
            return ModelToEntity(model.Uid.ToString(), "current", model);
        }

        protected override GroupEntity ModelToEntityForStaging(Group model)
        {
            return ModelToEntity(GetStagingPartitionKey(model.Uid), model.Uid.ToString(), model);
        }

        private GroupEntity ModelToEntity(string partitionKey, string rowKey, Group group)
        {
            return new GroupEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Group = JsonConvert.SerializeObject(group),
            };
        }
        
        private string GetStagingPartitionKey(long uid)
        {
            return $"staging{Math.Floor(uid / 5000d) * 5000}";
        }
    }
}