namespace CinemaSystem.Common.Exceptions;

public sealed class CloudinaryOperationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
