namespace Kolokwium.Exceptions;

public class CannotAddException : Exception
{
    public CannotAddException(string? message) : base(message)
    {
    }
}