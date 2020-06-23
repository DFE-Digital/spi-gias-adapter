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
    public class TableGroupRepository : TableCacheRepository<PointInTimeGroup, GroupEntity>,  IGroupRepository
    {
        public TableGroupRepository(CacheConfiguration configuration, ILoggerWrapper logger) 
            : base(configuration.TableStorageConnectionString, configuration.GroupTableName, logger, "groups")
        {
        }


        public async Task StoreAsync(PointInTimeGroup @group, CancellationToken cancellationToken)
        {
            await InsertOrUpdateAsync(group, cancellationToken);
        }

        public async Task StoreInStagingAsync(PointInTimeGroup[] groups, CancellationToken cancellationToken)
        {
            await InsertOrUpdateStagingAsync(groups, cancellationToken);
        }

        public async Task<PointInTimeGroup> GetGroupAsync(long uid, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(uid.ToString(), "current", cancellationToken);
        }

        public async Task<PointInTimeGroup> GetGroupFromStagingAsync(long uid, DateTime pointInTime, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(GetStagingPartitionKey(pointInTime), uid.ToString(), cancellationToken);
        }


        protected override GroupEntity ModelToEntity(PointInTimeGroup model)
        {
            return ModelToEntity(model.Uid.ToString(), "current", model);
        }

        protected override GroupEntity ModelToEntityForStaging(PointInTimeGroup model)
        {
            return ModelToEntity(GetStagingPartitionKey(model.PointInTime), model.Uid.ToString(), model);
        }

        protected override PointInTimeGroup EntityToModel(GroupEntity entity)
        {
            return JsonConvert.DeserializeObject<PointInTimeGroup>(entity.Group);
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
        
        private string GetStagingPartitionKey(DateTime pointInTime)
        {
            return $"staging{pointInTime:yyyMMdd}";
        }
    }
}