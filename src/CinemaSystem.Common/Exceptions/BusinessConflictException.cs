namespace CinemaSystem.Common.Exceptions;

public sealed class BusinessConflictException : Exception
{
    public BusinessConflictException(string message) : base(message)
    {
    }
}
