namespace Dfe.Spi.GiasAdapter.Domain.GiasApi
{
    public class Establishment
    {
        public long Urn { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"Urn: {Urn}, Name: {Name}";
        }
    }
}