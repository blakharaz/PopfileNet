using System;

namespace PopfileNet.Imap.Exceptions;

public class ImapOperationException : Exception
{
    public ImapOperationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
