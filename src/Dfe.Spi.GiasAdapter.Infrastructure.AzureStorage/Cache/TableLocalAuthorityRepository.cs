using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class TableLocalAuthorityRepository : TableCacheRepository<LocalAuthority, LocalAuthorityEntity>, ILocalAuthorityRepository
    {
        
        public TableLocalAuthorityRepository(CacheConfiguration configuration, ILoggerWrapper logger) 
            : base(configuration.TableStorageConnectionString, configuration.LocalAuthorityTableName, logger, "local authorities")
        {
        }
        
        

        public async Task StoreAsync(LocalAuthority localAuthority, CancellationToken cancellationToken)
        {
            await InsertOrUpdateAsync(localAuthority, cancellationToken);
        }

        public async Task StoreInStagingAsync(PointInTimeLocalAuthority[] localAuthorities, CancellationToken cancellationToken)
        {
            await InsertOrUpdateStagingAsync(localAuthorities, cancellationToken);
        }

        public async Task<LocalAuthority> GetLocalAuthorityAsync(int laCode, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(laCode.ToString(), "current", cancellationToken);
        }

        public async Task<LocalAuthority> GetLocalAuthorityFromStagingAsync(int laCode, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(GetStagingPartitionKey(laCode), laCode.ToString(), cancellationToken);
        }
        
        
        

        protected override LocalAuthorityEntity ModelToEntity(LocalAuthority model)
        {
            return ModelToEntity(model.Code.ToString(), "current", model);
        }

        protected override LocalAuthorityEntity ModelToEntityForStaging(LocalAuthority model)
        {
            return ModelToEntity(GetStagingPartitionKey(model.Code), model.Code.ToString(), model);
        }
        
        private LocalAuthorityEntity ModelToEntity(string partitionKey, string rowKey, LocalAuthority model)
        {
            return new LocalAuthorityEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                LocalAuthority = JsonConvert.SerializeObject(model),
            };
        }

        protected override LocalAuthority EntityToModel(LocalAuthorityEntity entity)
        {
            return JsonConvert.DeserializeObject<LocalAuthority>(entity.LocalAuthority);
        }
        
        private string GetStagingPartitionKey(int laCode)
        {
            return $"staging{Math.Floor(laCode / 50d) * 50}";
        }
    }
}