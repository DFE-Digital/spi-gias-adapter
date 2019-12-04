using System;

namespace Dfe.Spi.GiasAdapter.Infrastructure.GiasSoapApi
{
    public class SoapException : Exception
    {
        public string FaultCode { get; }

        public SoapException(string faultCode, string faultString)
            : base(faultString)
        {
            FaultCode = faultCode;
        }
    }
}