using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Domain;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using Dfe.Spi.GiasAdapter.Domain.Mapping;

namespace Dfe.Spi.GiasAdapter.Application.LearningProviders
{
    public interface ILearningProviderManager
    {
        Task<LearningProvider> GetLearningProviderAsync(string id, CancellationToken cancellationToken);
    }

    public class LearningProviderManager : ILearningProviderManager
    {
        private readonly IGiasApiClient _giasApiClient;
        private readonly IMapper _mapper;

        public LearningProviderManager(IGiasApiClient giasApiClient, IMapper mapper)
        {
            _giasApiClient = giasApiClient;
            _mapper = mapper;
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

            var learningProvider = await _mapper.MapAsync<LearningProvider>(establishment, cancellationToken);
            return learningProvider;
        }
    }
}