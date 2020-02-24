namespace Dfe.Spi.GiasAdapter.Domain.Configuration
{
    public class CacheConfiguration
    {
        public string TableStorageConnectionString { get; set; }
        public string EstablishmentTableName { get; set; }
        public string GroupTableName { get; set; }
        public string LocalAuthorityTableName { get; set; }
        
        public string ProcessingQueueConnectionString { get; set; }
    }
}