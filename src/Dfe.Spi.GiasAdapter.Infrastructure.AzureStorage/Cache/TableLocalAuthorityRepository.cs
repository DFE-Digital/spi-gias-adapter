using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class TableLocalAuthorityRepository : TableCacheRepository<PointInTimeLocalAuthority, LocalAuthorityEntity>, ILocalAuthorityRepository
    {
        
        public TableLocalAuthorityRepository(CacheConfiguration configuration, ILoggerWrapper logger) 
            : base(configuration.TableStorageConnectionString, configuration.LocalAuthorityTableName, logger, "local authorities")
        {
        }
        
        

        public async Task StoreAsync(PointInTimeLocalAuthority localAuthority, CancellationToken cancellationToken)
        {
            await StoreAsync(new[] {localAuthority}, cancellationToken);
        }

        public async Task StoreAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken)
        {
            await InsertOrUpdateAsync(localAuthorities, cancellationToken);
        }

        public async Task StoreInStagingAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken)
        {
            await InsertOrUpdateStagingAsync(localAuthorities, cancellationToken);
        }

        public async Task<PointInTimeLocalAuthority> GetLocalAuthorityAsync(int laCode, CancellationToken cancellationToken)
        {
            return await GetLocalAuthorityAsync(laCode, null, cancellationToken);
        }

        public async Task<PointInTimeLocalAuthority> GetLocalAuthorityAsync(int laCode, DateTime? pointInTime, CancellationToken cancellationToken)
        {
            if (!pointInTime.HasValue)
            {
                return await RetrieveAsync(laCode.ToString(), "current", cancellationToken);
            }

            var query = new TableQuery<LocalAuthorityEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, laCode.ToString()),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, pointInTime.Value.ToString("yyyyMMdd"))))
                .OrderByDesc("RowKey")
                .Take(1);
            var results = await QueryAsync(query, cancellationToken);

            // Appears to be a bug in the library that does not honor the order or the take.
            // Will reprocess here
            return results
                .OrderByDescending(r => r.PointInTime)
                .FirstOrDefault();
        }

        public async Task<PointInTimeLocalAuthority> GetLocalAuthorityFromStagingAsync(int laCode, DateTime pointInTime, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(GetStagingPartitionKey(pointInTime), laCode.ToString(), cancellationToken);
        }
        
        
        

        protected override LocalAuthorityEntity ModelToEntity(PointInTimeLocalAuthority model)
        {
            return ModelToEntity(model.Code.ToString(), model.PointInTime.ToString("yyyyMMdd"), model);
        }

        protected override LocalAuthorityEntity ModelToEntityForStaging(PointInTimeLocalAuthority model)
        {
            return ModelToEntity(GetStagingPartitionKey(model.PointInTime), model.Code.ToString(), model);
        }
        
        private LocalAuthorityEntity ModelToEntity(string partitionKey, string rowKey, PointInTimeLocalAuthority model)
        {
            return new LocalAuthorityEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                LocalAuthority = JsonConvert.SerializeObject(model),
                PointInTime = model.PointInTime,
                IsCurrent = model.IsCurrent,
            };
        }

        protected override PointInTimeLocalAuthority EntityToModel(LocalAuthorityEntity entity)
        {
            return JsonConvert.DeserializeObject<PointInTimeLocalAuthority>(entity.LocalAuthority);
        }

        protected override LocalAuthorityEntity[] ProcessEntitiesBeforeStoring(LocalAuthorityEntity[] entities)
        {
            var processedEntities = new List<LocalAuthorityEntity>();
            
            foreach (var entity in entities)
            {
                if (entity.IsCurrent)
                {
                    processedEntities.Add(new LocalAuthorityEntity
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = "current",
                        LocalAuthority = entity.LocalAuthority,
                        PointInTime = entity.PointInTime,
                        IsCurrent = entity.IsCurrent,
                    });
                }
                processedEntities.Add(entity);
            }

            return processedEntities.ToArray();
        }
        
        private string GetStagingPartitionKey(DateTime pointInTime)
        {
            return $"staging{pointInTime:yyyyMMdd}";
        }
    }
}