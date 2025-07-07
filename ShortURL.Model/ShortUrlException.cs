using System;

namespace ShortURL.Model
{
    public class ShortUrlException : Exception
    {
        public ShortUrlException(string message) : base(message)
        {
        }

        public ShortUrlException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ShortUrlException()
        {
        }
    }
}