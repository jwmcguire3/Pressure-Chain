using Godot;
using PressureChain.Core.Grid;

namespace PressureChain.Presentation;

internal static class HexLayout
{
    private const float SqrtThree = 1.7320508f;

    public static Vector2 CoordToPixel(HexCoord coord, float radius)
    {
        var x = radius * SqrtThree * (coord.Q + (coord.R * 0.5f));
        var y = radius * 1.5f * coord.R;
        return new Vector2(x, y);
    }

    public static Vector2[] CreatePointyTopPolygon(float radius)
    {
        var points = new Vector2[6];
        for (var index = 0; index < points.Length; index++)
        {
            var angle = Mathf.DegToRad(60f * index - 30f);
            points[index] = new Vector2(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
        }

        return points;
    }
}
