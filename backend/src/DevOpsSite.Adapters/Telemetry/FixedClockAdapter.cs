using DevOpsSite.Application.Ports;

namespace DevOpsSite.Adapters.Telemetry;

/// <summary>
/// Fixed clock for deterministic tests. Constitution §8.
/// </summary>
public sealed class FixedClockAdapter : IClockPort
{
    private DateTimeOffset _now;

    public FixedClockAdapter(DateTimeOffset fixedTime) => _now = fixedTime;

    public DateTimeOffset UtcNow => _now;

    public void Advance(TimeSpan duration) => _now = _now.Add(duration);
}
