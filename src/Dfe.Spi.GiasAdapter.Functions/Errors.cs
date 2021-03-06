namespace Dfe.Spi.GiasAdapter.Functions
{
    public static class Errors
    {
        public static readonly ErrorDetails GetLearningProvidersMalformedRequest = new ErrorDetails($"{CodePrefix}-PROVIDERS01", MalformedRequestMessage);
        public static readonly ErrorDetails GetLearningProvidersSchemaValidation = new ErrorDetails($"{CodePrefix}-PROVIDERS02", null);
        public static readonly ErrorDetails GetManagementGroupsMalformedRequest = new ErrorDetails($"{CodePrefix}-MGMTGRPS01", MalformedRequestMessage);
        public static readonly ErrorDetails GetManagementGroupsSchemaValidation = new ErrorDetails($"{CodePrefix}-MGMTGRPS02", null);
        public static readonly ErrorDetails GenericInvalidRequest = new ErrorDetails($"{CodePrefix}-REQ01", null);
        public static readonly ErrorDetails InvalidQueryParameter = new ErrorDetails($"{CodePrefix}-QS01", null);
        
        
        private const string CodePrefix = "SPI-GIAS";
        private const string MalformedRequestMessage = "The supplied body was either empty, or not well-formed JSON.";
    }

    public class ErrorDetails
    {
        public ErrorDetails(string code, string message)
        {
            Code = code;
            Message = message;
        }
        public string Code { get; }
        public string Message { get; }
    }
}