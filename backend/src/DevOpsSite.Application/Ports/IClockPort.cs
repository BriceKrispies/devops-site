namespace DevOpsSite.Application.Ports;

/// <summary>
/// Abstraction for time access. Constitution §8: Direct time access is forbidden in Domain/Application.
/// </summary>
public interface IClockPort
{
    DateTimeOffset UtcNow { get; }
}
