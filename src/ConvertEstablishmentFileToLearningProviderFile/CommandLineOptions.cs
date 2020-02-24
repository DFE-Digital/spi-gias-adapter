using CommandLine;

namespace ConvertEstablishmentFileToLearningProviderFile
{
    class CommandLineOptions
    {
        [Option('o', "output", Required = true, HelpText = "Path to output file to")]
        public string OutputPath { get; set; }
        
        [Option('t', "translator-url", Required = true, HelpText = "Base URL of translator API")]
        public string TranslatorBaseUrl { get; set; }
        
        [Option('k', "translator-subscription-key", Required = true, HelpText = "Subscription key of Translator API")]
        public string TranslatorSubscriptionKey { get; set; }
    }
}