using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using NodaTime.Text;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasCsvParsing
{
    public class DateTimeConverter : DefaultTypeConverter
    {
        private static readonly LocalDatePattern DatePatternDash = LocalDatePattern.CreateWithInvariantCulture("dd-MM-yyyy");
        private static readonly LocalDatePattern DatePatternSlash = LocalDatePattern.CreateWithInvariantCulture("dd/MM/yyyy");

        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrEmpty(text) || text == "NULL")
            {
                return null;
            }

            var parsed = DatePatternDash.Parse(text);
            if (!parsed.Success)
            {
                parsed = DatePatternSlash.Parse(text);
            }
            
            if (!parsed.Success)
            {
                throw new Exception($"Error parsing DateTime? on row {row.Context.Row} - {parsed.Exception}",
                    parsed.Exception);
            }

            return parsed.Value.ToDateTimeUnspecified();
        }
    }
}