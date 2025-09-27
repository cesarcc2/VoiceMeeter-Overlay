namespace VMHud.Core.Models;

public sealed class MatrixState
{
    public IReadOnlyList<InputStrip> Strips { get; init; } = Array.Empty<InputStrip>();
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<string> BusNames { get; init; } = new[] { "A1", "A2", "A3", "A4", "A5", "B1", "B2", "B3" };
    public int PhysicalInputCount { get; init; } = 0;
    public IReadOnlyList<float> StripGains { get; init; } = Array.Empty<float>();
    public IReadOnlyList<float> BusGains { get; init; } = Array.Empty<float>();
}
