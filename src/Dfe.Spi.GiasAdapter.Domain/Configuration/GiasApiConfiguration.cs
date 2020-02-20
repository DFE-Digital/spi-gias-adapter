namespace Dfe.Spi.GiasAdapter.Domain.Configuration
{
    public class GiasApiConfiguration
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        
        public int ExtractId { get; set; }
        public string ExtractEstablishmentsFileName { get; set; }
    }
}