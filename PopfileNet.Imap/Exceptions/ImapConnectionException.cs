using System;

namespace PopfileNet.Imap.Exceptions;

public class ImapConnectionException : Exception
{
    public ImapConnectionException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
