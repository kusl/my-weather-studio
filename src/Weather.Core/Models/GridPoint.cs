namespace Weather.Core.Models;

public readonly record struct GridPoint(string GridId, int GridX, int GridY)
{
    public override string ToString() => $"{GridId}/{GridX},{GridY}";
}
