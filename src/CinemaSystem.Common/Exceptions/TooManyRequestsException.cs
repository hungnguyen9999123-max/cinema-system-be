namespace CinemaSystem.Common.Exceptions;

public sealed class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message, int retryAfterSeconds)
        : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int RetryAfterSeconds { get; }
}
