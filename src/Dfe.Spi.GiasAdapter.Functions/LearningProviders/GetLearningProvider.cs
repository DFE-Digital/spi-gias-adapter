using System;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dfe.Spi.GiasAdapter.Functions.LearningProviders
{
    public class GetLearningProvider
    {
        private const string FunctionName = nameof(GetLearningProvider);

        private readonly ILearningProviderManager _learningProviderManager;
        private readonly ILogger _logger;

        public GetLearningProvider(ILearningProviderManager learningProviderManager, ILogger logger)
        {
            _learningProviderManager = learningProviderManager;
            _logger = logger;
        }

        [FunctionName(FunctionName)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "learning-providers/{id}")]
            HttpRequest req,
            string id,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{FunctionName} triggered at {DateTime.Now} with id {id}");

            try
            {
                var learningProvider = await _learningProviderManager.GetLearningProviderAsync(id, cancellationToken);

                return learningProvider == null
                    ? (IActionResult)new NotFoundResult()
                    : (IActionResult)new OkObjectResult(learningProvider);
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}