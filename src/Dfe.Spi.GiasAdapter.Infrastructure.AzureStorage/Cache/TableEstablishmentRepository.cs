using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Domain.Cache;
using Dfe.Spi.GiasAdapter.Domain.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Infrastructure.AzureStorage.Cache
{
    public class TableEstablishmentRepository : TableCacheRepository<Establishment, EstablishmentEntity>, IEstablishmentRepository
    {
        
        public TableEstablishmentRepository(CacheConfiguration configuration, ILoggerWrapper logger) 
            : base(configuration.TableStorageConnectionString, configuration.EstablishmentTableName, logger, "establishments")
        {
        }
        
        

        public async Task StoreAsync(Establishment establishment, CancellationToken cancellationToken)
        {
            await InsertOrUpdateAsync(establishment, cancellationToken);
        }

        public async Task StoreAsync(Establishment[] establishments, CancellationToken cancellationToken)
        {
            await InsertOrUpdateAsync(establishments, cancellationToken);
        }

        public async Task StoreInStagingAsync(PointInTimeEstablishment[] establishments, CancellationToken cancellationToken)
        {
            await InsertOrUpdateStagingAsync(establishments, cancellationToken);
        }

        public async Task<Establishment> GetEstablishmentAsync(long urn, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(urn.ToString(), "current", cancellationToken);
        }

        public async Task<Establishment> GetEstablishmentFromStagingAsync(long urn, CancellationToken cancellationToken)
        {
            return await RetrieveAsync(GetStagingPartitionKey(urn), urn.ToString(), cancellationToken);
        }
        
        

        protected override EstablishmentEntity ModelToEntity(Establishment model)
        {
            return ModelToEntity(model.Urn.ToString(), "current", model);
        }

        protected override EstablishmentEntity ModelToEntityForStaging(Establishment model)
        {
            return ModelToEntity(GetStagingPartitionKey(model.Urn), model.Urn.ToString(), model);
        }

        private EstablishmentEntity ModelToEntity(string partitionKey, string rowKey, Establishment establishment)
        {
            return new EstablishmentEntity
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                Establishment = JsonConvert.SerializeObject(establishment),
            };
        }

        protected override Establishment EntityToModel(EstablishmentEntity entity)
        {
            return JsonConvert.DeserializeObject<Establishment>(
                entity.Establishment);
        }
        
        private string GetStagingPartitionKey(long urn)
        {
            return $"staging{Math.Floor(urn / 5000d) * 5000}";
        }
    }
}