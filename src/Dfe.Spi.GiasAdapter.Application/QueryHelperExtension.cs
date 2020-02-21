using System.Linq;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Models.Entities;

namespace Dfe.Spi.GiasAdapter.Application
{
    internal static class QueryHelperExtension
    {
        internal static T Pick<T>(this T source, string fields) where T : EntityBase
        {
            // Then we need to limit the fields we send back...
            var requestedFields = fields
                .Split(',')
                .Select(x => x.ToUpperInvariant())
                .ToArray();

            var pruned = source.PruneModel(requestedFields);

            // If lineage was requested then...
            if (pruned._Lineage != null)
            {
                // ... prune the lineage too.
                pruned._Lineage = pruned
                    ._Lineage
                    .Where(x => requestedFields.Contains(x.Key.ToUpperInvariant()))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            return pruned;
        }
    }
}