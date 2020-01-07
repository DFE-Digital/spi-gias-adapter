namespace Dfe.Spi.GiasAdapter.Domain.Configuration
{
    public class CacheConfiguration
    {
        public string TableStorageConnectionString { get; set; }
        public string EstablishmentTableName { get; set; }
        
        public string EstablishmentProcessingQueueConnectionString { get; set; }
    }
}