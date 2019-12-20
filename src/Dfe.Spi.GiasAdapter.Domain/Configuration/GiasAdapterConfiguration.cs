namespace Dfe.Spi.GiasAdapter.Domain.Configuration
{
    public class GiasAdapterConfiguration
    {
        public GiasApiConfiguration GiasApi { get; set; } = new GiasApiConfiguration();
        public CacheConfiguration Cache { get; set; }
    }
}