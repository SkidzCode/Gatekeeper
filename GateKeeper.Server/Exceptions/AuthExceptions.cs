using System;

namespace GateKeeper.Server.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException(string message) : base(message) { }

        public InvalidCredentialsException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message) { }

        public UserNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class AccountLockedException : Exception
    {
        public AccountLockedException(string message) : base(message) { }

        public AccountLockedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class SessionExpiredException : Exception
    {
        public SessionExpiredException(string message) : base(message) { }

        public SessionExpiredException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class InvalidTokenException : Exception
    {
        public InvalidTokenException(string message) : base(message) { }

        public InvalidTokenException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class RegistrationException : Exception
    {
        public RegistrationException(string message) : base(message) { }

        public RegistrationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
