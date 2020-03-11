using Dfe.Spi.GiasAdapter.Domain.GiasApi;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi
{
    internal static class XElementExtensions
    {
        internal static XElement GetElementByLocalName(this XElement containerElement, string localName)
        {
            return containerElement.Elements().SingleOrDefault(e => e.Name.LocalName == localName);
        }

        internal static string GetValueFromChildElement(this XElement containerElement, string localName)
        {
            XElement element = containerElement.GetElementByLocalName(localName);

            if (element == null || string.IsNullOrEmpty(element.Value))
            {
                return null;
            }

            return element.Value;
        }

        internal static CodeNamePair GetCodeNamePairFromChildElement(this XElement containerElement, string localName)
        {
            XElement element = containerElement.GetElementByLocalName(localName);

            if (element == null)
            {
                return null;
            }

            return new CodeNamePair
            {
                Code = element.GetValueFromChildElement("Code"),
                DisplayName = element.GetValueFromChildElement("DisplayName"),
            };
        }

        internal static DateTime? GetDateTimeFromChildElement(this XElement containerElement, string localName, bool includeTime = false)
        {
            string value = containerElement.GetValueFromChildElement(localName);

            if (value == null)
            {
                return null;
            }
 
            var dateTime = DateTime.Parse(value);
            return includeTime ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        internal static long? GetLongFromChildElement(this XElement containerElement, string localName)
        {
            string value = containerElement.GetValueFromChildElement(localName);

            if (value == null)
            {
                return null;
            }

            return long.Parse(value);
        }

        internal static decimal? GetDecimalFromChildElement(this XElement containerElement, string localName)
        {
            string value = containerElement.GetValueFromChildElement(localName);

            if (value == null)
            {
                return null;
            }

            return decimal.Parse(value);
        }
    }
}