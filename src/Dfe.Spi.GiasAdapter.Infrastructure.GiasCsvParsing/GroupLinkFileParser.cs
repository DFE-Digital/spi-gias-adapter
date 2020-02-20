using System.IO;
using CsvHelper.Configuration;
using Dfe.Spi.GiasAdapter.Domain.GiasApi;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasCsvParsing
{
    public class GroupLinkFileParser : CsvFileParser<GroupLink>
    {
        private class GroupLinkCsvMapping : ClassMap<GroupLink>
        {
            public GroupLinkCsvMapping()
            {
                var dateTimeConverter = new DateTimeConverter();

                Map(x => x.Uid).Name("Linked UID");
                Map(x => x.Urn).Name("URN");
                Map(x => x.GroupType).Name("Group Type");
            }
        }

        public GroupLinkFileParser(StreamReader reader)
            : base(reader, new GroupLinkCsvMapping())
        {
        }
    }
}