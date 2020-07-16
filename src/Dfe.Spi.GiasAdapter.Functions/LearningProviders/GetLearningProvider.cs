using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.LearningProviders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Dfe.Spi.Common.Http.Server.Definitions;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Functions.LearningProviders
{
    public class GetLearningProvider
    {
        private const string FunctionName = nameof(GetLearningProvider);

        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly ILearningProviderManager _learningProviderManager;
        private readonly ILoggerWrapper _logger;

        public GetLearningProvider(IHttpSpiExecutionContextManager httpSpiExecutionContextManager, ILearningProviderManager learningProviderManager, ILoggerWrapper logger)
        {
            _learningProviderManager = learningProviderManager;
            _logger = logger;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
        }

        [FunctionName(FunctionName)]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "learning-providers/{id}")]
            HttpRequest req,
            string id,
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetContext(req.Headers);

            var fields = (string)req.Query["fields"];
            var live = ((string) req.Query["live"] ?? "").ToLower();
            var readFromLive = live == "true" || live == "yes" || live == "1";
            var pointInTimeString = (string) req.Query["pointInTime"];
            DateTime? pointInTime;

            try
            {
                pointInTime = string.IsNullOrEmpty(pointInTimeString)
                    ? null
                    : (DateTime?) pointInTimeString.ToDateTime();
            }
            catch (InvalidDateTimeFormatException ex)
            {
                return new HttpErrorBodyResult(
                    HttpStatusCode.BadRequest,
                    Errors.InvalidQueryParameter.Code,
                    ex.Message);
            }

            _logger.Info($"{FunctionName} triggered at {DateTime.Now} with id {id}");

            try
            {
                var learningProvider = await _learningProviderManager.GetLearningProviderAsync(id, fields, readFromLive, pointInTime, cancellationToken);

                if (learningProvider == null)
                {
                    _logger.Info($"{FunctionName} found no learning provider with id {id}. Returning not found");
                    return new NotFoundResult();
                }

                _logger.Info($"{FunctionName} found learning provider with id {id}. Returning ok");
                return new FormattedJsonResult(learningProvider);
            }
            catch (ArgumentException ex)
            {
                _logger.Info($"{FunctionName} returning bad request: {ex.Message}");
                return new HttpErrorBodyResult(
                    HttpStatusCode.BadRequest,
                    Errors.GenericInvalidRequest.Code,
                    ex.Message);
            }
        }
    }
}