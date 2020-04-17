using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dfe.Spi.Common.Http.Server;
using Dfe.Spi.Common.Http.Server.Definitions;
using Dfe.Spi.Common.Logging.Definitions;
using Dfe.Spi.Common.Models;
using Dfe.Spi.GiasAdapter.Application.ManagementGroups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace Dfe.Spi.GiasAdapter.Functions.ManagementGroups
{
    public class GetManagementGroups : FunctionsBase<GetManagementGroupsRequest>
    {
        private const string FunctionName = nameof(GetManagementGroups);

        private readonly IHttpSpiExecutionContextManager _httpSpiExecutionContextManager;
        private readonly IManagementGroupManager _managementGroupManager;
        private readonly ILoggerWrapper _logger;

        public GetManagementGroups(
            IHttpSpiExecutionContextManager httpSpiExecutionContextManager, 
            IManagementGroupManager managementGroupManager, 
            ILoggerWrapper logger)
            : base(httpSpiExecutionContextManager, logger)
        {
            _managementGroupManager = managementGroupManager;
            _logger = logger;
            _httpSpiExecutionContextManager = httpSpiExecutionContextManager;
        }
        
        [FunctionName(FunctionName)]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "management-groups")]
            HttpRequest req,
            CancellationToken cancellationToken)
        {
            return await ValidateAndRunAsync(req, null, cancellationToken);
        }

        protected override HttpErrorBodyResult GetMalformedErrorResponse(FunctionRunContext runContext)
        {
            return new HttpErrorBodyResult(
                HttpStatusCode.BadRequest,
                Errors.GetManagementGroupsMalformedRequest.Code,
                Errors.GetLearningProvidersMalformedRequest.Message);
        }

        protected override HttpErrorBodyResult GetSchemaValidationResponse(JsonSchemaValidationException validationException, FunctionRunContext runContext)
        {
            return new HttpSchemaValidationErrorBodyResult(Errors.GetManagementGroupsSchemaValidation.Code, validationException);
        }

        protected override async Task<IActionResult> ProcessWellFormedRequestAsync(GetManagementGroupsRequest request, FunctionRunContext runContext,
            CancellationToken cancellationToken)
        {
            var providers = await _managementGroupManager.GetManagementGroupsAsync(request.Identifiers, request.Fields, cancellationToken);
            
            if (JsonConvert.DefaultSettings != null)
            {
                return new JsonResult(
                    providers,
                    JsonConvert.DefaultSettings())
                {
                    StatusCode = 200,
                };
            }
            else
            {
                return new JsonResult(providers)
                {
                    StatusCode = 200,
                };
            }
        }
    }

    public class GetManagementGroupsRequest : RequestResponseBase
    {
        public string[] Identifiers { get; set; }
        public string[] Fields { get; set; }
    }
}