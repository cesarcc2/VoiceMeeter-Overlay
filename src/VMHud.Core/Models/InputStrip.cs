namespace VMHud.Core.Models;

public sealed class InputStrip
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    // A1..A5, B1..B3 => 8 flags
    public bool[] Outputs { get; } = new bool[8];
}

