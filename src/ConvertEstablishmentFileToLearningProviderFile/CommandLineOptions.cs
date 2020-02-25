using CommandLine;

namespace ConvertEstablishmentFileToLearningProviderFile
{
    class CommandLineOptions
    {
        [Option('e', "token-endpoint", Required = true, HelpText = "An OAuth token endpoint.")]
        public string TokenEndpoint { get; set; }

        [Option('c', "client-id", Required = true, HelpText = "An OAuth client id.")]
        public string ClientId { get; set; }

        [Option('s', "client-secret", Required = true, HelpText = "An OAuth client secret.")]
        public string ClientSecret { get; set; }

        [Option('r', "resource", Required = true, HelpText = "An OAuth resource.")]
        public string Resource { get; set; }

        [Option('o', "output", Required = true, HelpText = "Path to output file to")]
        public string OutputPath { get; set; }
        
        [Option('t', "translator-url", Required = true, HelpText = "Base URL of translator API")]
        public string TranslatorBaseUrl { get; set; }
        
        [Option('k', "translator-subscription-key", Required = true, HelpText = "Subscription key of Translator API")]
        public string TranslatorSubscriptionKey { get; set; }
    }
}