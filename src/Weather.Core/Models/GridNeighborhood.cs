namespace Weather.Core.Models;

public static class GridNeighborhood
{
    public static IReadOnlyList<GridPoint> Surrounding(GridPoint origin, int radius = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);

        var side = (2 * radius) + 1;
        List<GridPoint> cells = [with(capacity: (side * side) - 1)];

        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dy = -radius; dy <= radius; dy++)
            {
                var x = origin.GridX + dx;
                var y = origin.GridY + dy;
                if ((dx, dy) is (0, 0) || x < 0 || y < 0)
                {
                    continue;
                }

                cells.Add(origin with { GridX = x, GridY = y });
            }
        }

        return cells;
    }
}
