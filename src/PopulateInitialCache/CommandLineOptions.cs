using CommandLine;

namespace PopulateInitialCache
{
    class CommandLineOptions
    {
        [Option('s', "storage-connection-string", Required = true, HelpText = "Azure storage connection string")]
        public string StorageConnectionString { get; set; }

        [Option("establishments-table-name", Required = false, Default = "establishments", HelpText = "Establishments table name")]
        public string EstablishmentsTableName { get; set; }

        [Option("groups-table-name", Required = false, Default = "groups", HelpText = "Groups table name")]
        public string GroupsTableName { get; set; }

        [Option("las-table-name", Required = false, Default = "localauthorities", HelpText = "Local authorities table name")]
        public string LocalAuthoritiesTableName { get; set; }

        [Option('e', "gias-soap-endpoint", Required = true, HelpText = "URL for the GIAS SOAP endpoint")]
        public string GiasSoapEndpoint { get; set; }

        [Option('u', "gias-soap-username", Required = true, HelpText = "Username for the GIAS SOAP endpoint")]
        public string GiasSoapUsername { get; set; }

        [Option('p', "gias-soap-password", Required = true, HelpText = "Password for the GIAS SOAP endpoint")]
        public string GiasSoapPassword { get; set; }

        [Option('i', "extract-id", Required = true, HelpText = "ID of the extract to download")]
        public int ExtractId { get; set; }

        [Option("extract-establishments-file", Required = false, Default = "Eapim_Daily_Download.csv", HelpText = "File name of the establishments file in the extract")]
        public string EstablishmentsFileName { get; set; }

        [Option("extract-groups-file", Required = false, Default = "groups.csv", HelpText = "File name of the groups file in the extract")]
        public string GroupsFileName { get; set; }

        [Option("extract-links-file", Required = false, Default = "groupLinks.csv", HelpText = "File name of the group links file in the extract")]
        public string GroupLinksFileName { get; set; }
    }
}