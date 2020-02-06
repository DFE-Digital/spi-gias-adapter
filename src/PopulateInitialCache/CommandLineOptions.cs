using CommandLine;

namespace PopulateInitialCache
{
    class CommandLineOptions
    {
        [Option('s', "storage-connection-string", Required = true, HelpText = "Azure storage connection string")]
        public string StorageConnectionString { get; set; }

        [Option('t', "table-name", Required = false, Default = "establishments", HelpText = "Establishments table name")]
        public string EstablishmentsTableName { get; set; }
    }
}