namespace TmsApi.Application.Common;

public sealed class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}