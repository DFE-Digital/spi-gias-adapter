using System.Threading;
using System.Threading.Tasks;

namespace Dfe.Spi.GiasAdapter.Domain.Translation
{
    public interface ITranslator
    {
        Task<string> TranslateEnumValue(string enumName, string sourceValue, CancellationToken cancellationToken);
    }
}