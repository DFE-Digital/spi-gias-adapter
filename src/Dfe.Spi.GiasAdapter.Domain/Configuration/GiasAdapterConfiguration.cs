namespace Dfe.Spi.GiasAdapter.Domain.Configuration
{
    public class GiasAdapterConfiguration
    {
        public AuthenticationConfiguration Authentication { get; set; } = new AuthenticationConfiguration();
        public GiasApiConfiguration GiasApi { get; set; } = new GiasApiConfiguration();
        public CacheConfiguration Cache { get; set; } = new CacheConfiguration();
        public MiddlewareConfiguration Middleware { get; set; } = new MiddlewareConfiguration();
        public TranslatorConfiguration Translator { get; set; } = new TranslatorConfiguration();
    }
}