using System;

namespace Services
{
    public class FunctionalException : Exception
    {
        public FunctionalException()
        {
        }

        public FunctionalException(string code, string message)
            : base($"{code}: {message}")
        {
        }

        public FunctionalException(string code, string message, Exception inner)
            : base($"{code}: {message}", inner)
        {
        }
    }
}