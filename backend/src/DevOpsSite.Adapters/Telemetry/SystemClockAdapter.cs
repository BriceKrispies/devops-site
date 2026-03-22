using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.Telemetry;

public sealed class SystemClockAdapter : IClockPort
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
