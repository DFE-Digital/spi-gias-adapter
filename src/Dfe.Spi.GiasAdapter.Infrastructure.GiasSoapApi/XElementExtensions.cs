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
    }
}