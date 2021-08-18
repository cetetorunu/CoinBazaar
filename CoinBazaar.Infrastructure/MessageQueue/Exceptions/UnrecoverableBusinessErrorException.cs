using System;
using System.Runtime.Serialization;

namespace CoinBazaar.Infrastructure.MessageQueue.Camunda
{
    [Serializable]
    public class UnrecoverableBusinessErrorException : Exception, ISerializable
    {
        public string BusinessErrorCode { get; set; }

        public UnrecoverableBusinessErrorException(string businessErrorCode, string message) : base(message)
        {
            BusinessErrorCode = businessErrorCode;
        }

        public UnrecoverableBusinessErrorException(string businessErrorCode, string message, Exception innerException) : base(message, innerException)
        {
            BusinessErrorCode = businessErrorCode;
        }

    }
}