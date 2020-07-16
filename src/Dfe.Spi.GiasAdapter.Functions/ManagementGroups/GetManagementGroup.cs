using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Extensions;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.GiasAdapter.Application.ManagementGroups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Functions.ManagementGroups
{
    public class GetManagementGroup
    {
        private const string FunctionName = nameof(GetManagementGroup);

        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly IManagementGroupManager _managementGroupManager;
        private readonly ILoggerWrapper _logger;

        public GetManagementGroup(IHttpSpiExecutionContextManager httpSpiExecutionContextManager, IManagementGroupManager managementGroupManager, ILoggerWrapper logger)
        {
            _managementGroupManager = managementGroupManager;
            _logger = logger;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
        }
        
        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "management-groups/{id}")]
            HttpRequest req,
            string id,
            CancellationToken cancellationToken)
        {
            _httpSpiExecutionContextManager.SetContext(req.Headers);
            
            _logger.Info($"{FunctionName} triggered at {DateTime.Now} with id {id}");
            
            var fields = req.Query["fields"];
            DateTime? pointInTime;
            try
            {
                var pointInTimeString = (string) req.Query["pointInTime"];
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

            try
            {
                var managementGroup = await _managementGroupManager.GetManagementGroupAsync(id, fields, pointInTime, cancellationToken);

                if (managementGroup == null)
                {
                    _logger.Info($"{FunctionName} found no management group with id {id}. Returning not found");
                    return new NotFoundResult();
                }

                _logger.Info($"{FunctionName} found management group with id {id}. Returning ok");
                return new FormattedJsonResult(managementGroup);
            }
            catch (ArgumentException ex)
            {
                _logger.Info($"{FunctionName} returning bad request: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}