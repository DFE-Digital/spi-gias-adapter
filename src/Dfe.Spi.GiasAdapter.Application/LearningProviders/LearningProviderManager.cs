using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Application.LearningProviders
{
    public interface ILearningProviderManager
    {
        Task<LearningProvider> GetLearningProviderAsync(string id, CancellationToken cancellationToken);
    }

    public class LearningProviderManager : ILearningProviderManager
    {
        private readonly IGiasApiClient _giasApiClient;

        public LearningProviderManager(IGiasApiClient giasApiClient)
        {
            _giasApiClient = giasApiClient;
        }

        public async Task<LearningProvider> GetLearningProviderAsync(string id, CancellationToken cancellationToken)
        {
            int urn;
            if (!int.TryParse(id, out urn))
            {
                throw new ArgumentException($"id must be a number (urn) but received {id}", nameof(id));
            }

            var establishment = await _giasApiClient.GetEstablishmentAsync(urn, cancellationToken);
            if (establishment == null)
            {
                return null;
            }

            // TODO: Offload to mapper class
            return new LearningProvider
            {
                Name = establishment.Name,
            };
        }
    }
}