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
    public class TableEstablishmentRepository : TableCacheRepository<PointInTimeEstablishment, EstablishmentEntity>, IEstablishmentRepository
    {
        
        public TableEstablishmentRepository(CacheConfiguration configuration, ILoggerWrapper logger) 
            : base(configuration.TableStorageConnectionString, configuration.EstablishmentTableName, logger, "establishments")
        {
        }
        
        

        public async Task StoreAsync(PointInTimeEstablishment establishment, CancellationToken cancellationToken)
        {
            await StoreAsync(new[] {establishment}, cancellationToken);
        }

        public async Task StoreAsync(PointInTimeEstablishment[] establishments, CancellationToken cancellationToken)
        {
            await InsertOrUpdateAsync(establishments, cancellationToken);
        }

        public async Task StoreInStagingAsync(PointInTimeEstablishment[] establishments, CancellationToken cancellationToken)
        {
            await InsertOrUpdateStagingAsync(establishments, cancellationToken);
        }

        public async Task<PointInTimeEstablishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken)
        {
            return await GetEstablishmentAsync(urn, null, cancellationToken);
        }

        public async Task<PointInTimeEstablishment> GetEstablishmentAsync(long urn, DateTime? pointInTime, CancellationToken cancellationToken)
        {
            if (!pointInTime.HasValue)
            {
                return await RetrieveAsync(urn.ToString(), "current", cancellationToken);
            }

            var query = new TableQuery<EstablishmentEntity>()
                .Where(TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, urn.ToString()),
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

        public async Task<PointInTimeEstablishment> GetEstablishmentFromStagingAsync(long urn, DateTime pointInTime, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(GetStagingPartitionKey(pointInTime), urn.ToString(), cancellationToken);
        }
        
        

        protected override EstablishmentEntity ModelToEntity(PointInTimeEstablishment model)
        {
            return ModelToEntity(model.Urn.ToString(), model.PointInTime.ToString("yyyyMMdd"), model);
        }

        protected override EstablishmentEntity ModelToEntityForStaging(PointInTimeEstablishment model)
        {
            return ModelToEntity(GetStagingPartitionKey(model.PointInTime), model.Urn.ToString(), model);
        }

        private EstablishmentEntity ModelToEntity(string partitionKey, string rowKey, PointInTimeEstablishment establishment)
        {
            return new EstablishmentEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Establishment = JsonConvert.SerializeObject(establishment),
                PointInTime = establishment.PointInTime,
                IsCurrent = establishment.IsCurrent,
            };
        }

        protected override PointInTimeEstablishment EntityToModel(EstablishmentEntity entity)
        {
            return JsonConvert.DeserializeObject<PointInTimeEstablishment>(
                entity.Establishment);
        }

        protected override EstablishmentEntity[] ProcessEntitiesBeforeStoring(EstablishmentEntity[] entities)
        {
            var processedEntities = new List<EstablishmentEntity>();
            
            foreach (var entity in entities)
            {
                if (entity.IsCurrent)
                {
                    processedEntities.Add(new EstablishmentEntity
                    {
                        PartitionKey = entity.PartitionKey,
                        RowKey = "current",
                        Establishment = entity.Establishment,
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