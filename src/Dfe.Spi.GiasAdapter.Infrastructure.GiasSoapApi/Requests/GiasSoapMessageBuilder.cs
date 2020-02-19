using System;
using System.Xml.Linq;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi.Requests
{
    internal interface IGiasSoapMessageBuilder<TParameters>
    {
        string Build(TParameters parameters);
    }

    internal abstract class GiasSoapMessageBuilder<TParameters> : IGiasSoapMessageBuilder<TParameters>
    {
        protected static readonly string UsernameTokenType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";
        protected static readonly string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        protected static readonly XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        protected static readonly XNamespace giasNs = "http://ws.edubase.texunatech.com";
        protected static readonly XNamespace wsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        protected static readonly XNamespace wsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";

        protected GiasSoapMessageBuilder(string username, string password, int messageValidForSeconds = 30)
        {
            Username = username;
            Password = password;
            MessageValidForSeconds = messageValidForSeconds;
        }
        
        protected string Username { get; }
        protected string Password { get; }
        protected int MessageValidForSeconds { get; }

        public virtual string Build(TParameters parameters)
        {
            var envelope = BuildEnvelop(parameters);
            return envelope.ToString();
        }

        protected virtual XElement BuildEnvelop(TParameters parameters)
        {
            return new XElement(soapNs + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", soapNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ws", giasNs.NamespaceName),
                BuildHeader(parameters),
                BuildBody(parameters));
        }

        protected virtual XElement BuildHeader(TParameters parameters)
        {
            return new XElement(soapNs + "Header",
                BuildSecurityHeader(parameters));
        }

        protected virtual XElement BuildSecurityHeader(TParameters parameters)
        {
            return new XElement(wsseNs + "Security",
                new XAttribute(XNamespace.Xmlns + "wsse", wsseNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "wsu", wsuNs.NamespaceName),
                BuildUsernameTokenSecurityHeader(parameters),
                BuildTimestampSecurityHeader(parameters));
        }

        protected virtual XElement BuildUsernameTokenSecurityHeader(TParameters parameters)
        {
            return new XElement(wsseNs + "UsernameToken",
                new XAttribute(wsuNs + "Id", $"UsernameToken-{Guid.NewGuid():N}"),
                new XElement(wsseNs + "Username", Username),
                new XElement(wsseNs + "Password",
                    new XAttribute("Type", UsernameTokenType),
                    new XText(Password)));
        }

        protected virtual XElement BuildTimestampSecurityHeader(TParameters parameters)
        {
            return new XElement(wsuNs + "Timestamp",
                new XAttribute(wsuNs + "Id", $"TS-{Guid.NewGuid():N}"),
                new XElement(wsuNs + "Created", DateTime.UtcNow.ToString(DateTimeFormat)),
                new XElement(wsuNs + "Expires",
                    DateTime.UtcNow.AddSeconds(MessageValidForSeconds).ToString(DateTimeFormat)));
        }

        protected abstract XElement BuildBody(TParameters parameters);

    }
}