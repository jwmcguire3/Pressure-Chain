using Godot;
using PressureChain.Core.Board;
using PressureChain.Core.Grid;
using BoardCell = PressureChain.Core.Board.Node;

namespace PressureChain.Presentation;

public partial class HexCellNode : Node2D
{
    private static readonly Color StableColor = new("#2E5F6E");
    private static readonly Color SwellingColor = new("#C99A4D");
    private static readonly Color CriticalColor = new("#E06B2B");
    private static readonly Color VolatileColor = new("#F2F2F2");
    private static readonly Color BurstColor = Colors.White;
    private static readonly Color VolatileOutlineColor = new("#C63B3B");
    private static readonly Color SelectionOutlineColor = new("#6AD7E8");
    private static readonly Color DefaultOutlineColor = new("#16313A");
    private static readonly Color SymbolColor = new("#13252B");

    private readonly Vector2[] _hexPoints = HexLayout.CreatePointyTopPolygon(48f);

    private BoardCell _model;
    private BoardCell _displayModel;
    private float _pulseTime;
    private bool _isSelected;
    private bool _isBurstAnimating;
    private double _burstAnimationToken;

    public HexCoord Coord { get; private set; }

    public override void _Ready()
    {
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_isBurstAnimating)
        {
            return;
        }

        _pulseTime += (float)delta;
        var frequency = 1f + (_displayModel.Pressure / 25f);
        var pulse = 0.5f + (0.5f * Mathf.Sin(_pulseTime * Mathf.Tau * frequency));
        var scaleFactor = Mathf.Lerp(1f, 1.05f, pulse);
        Scale = Vector2.One * scaleFactor;
    }

    public void Bind(HexCoord coord, BoardCell model)
    {
        Coord = coord;
        UpdateModel(model);
    }

    public void UpdateModel(BoardCell model)
    {
        _model = model;
        _displayModel = model;
        QueueRedraw();
    }

    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
        {
            return;
        }

        _isSelected = selected;
        QueueRedraw();
    }

    public bool ContainsLocalPoint(Vector2 localPoint)
    {
        return Geometry2D.IsPointInPolygon(localPoint, _hexPoints);
    }

    public async System.Threading.Tasks.Task PlayBurstAnimationAsync()
    {
        _burstAnimationToken += 1d;
        var token = _burstAnimationToken;
        _isBurstAnimating = true;
        Scale = Vector2.One * 1.3f;

        var burstModel = _displayModel with { Pressure = 100 };
        _displayModel = burstModel;
        QueueRedraw();
        await ToSignal(GetTree().CreateTimer(0.08d), SceneTreeTimer.SignalName.Timeout);
        if (!Mathf.IsEqualApprox((float)token, (float)_burstAnimationToken))
        {
            return;
        }

        _displayModel = _model with { Pressure = 0 };
        QueueRedraw();
        await ToSignal(GetTree().CreateTimer(0.07d), SceneTreeTimer.SignalName.Timeout);
        if (!Mathf.IsEqualApprox((float)token, (float)_burstAnimationToken))
        {
            return;
        }

        _isBurstAnimating = false;
        Scale = Vector2.One;
    }

    public override void _Draw()
    {
        DrawColoredPolygon(_hexPoints, GetFillColor());
        DrawPolyline(_hexPoints.Append(_hexPoints[0]).ToArray(), GetOutlineColor(), 3f, antialiased: true);
        DrawTypeSymbol();
        DrawPressureLabel();
    }

    private Color GetFillColor()
    {
        return NodeStateRules.FromPressure(_displayModel.Pressure) switch
        {
            NodeState.Stable => StableColor,
            NodeState.Swelling => SwellingColor,
            NodeState.Critical => CriticalColor,
            NodeState.Volatile => VolatileColor,
            NodeState.Burst => BurstColor,
            _ => StableColor
        };
    }

    private Color GetOutlineColor()
    {
        if (_isSelected)
        {
            return SelectionOutlineColor;
        }

        return NodeStateRules.FromPressure(_displayModel.Pressure) == NodeState.Volatile
            ? VolatileOutlineColor
            : DefaultOutlineColor;
    }

    private void DrawTypeSymbol()
    {
        switch (_displayModel.Type)
        {
            case NodeType.Vent:
                DrawVentArrow();
                break;
            case NodeType.Bulwark:
                DrawBulwarkMark();
                break;
            case NodeType.Amplifier:
                DrawAmplifierMark();
                break;
        }
    }

    private void DrawVentArrow()
    {
        if (_displayModel.Facing is null)
        {
            return;
        }

        var direction = HexLayout.CoordToPixel(_displayModel.Facing.Value.Offset(), 14f).Normalized();
        var start = direction * -8f;
        var end = direction * 18f;
        var left = end + direction.Rotated(Mathf.DegToRad(150f)) * 10f;
        var right = end + direction.Rotated(Mathf.DegToRad(-150f)) * 10f;

        DrawLine(start, end, SymbolColor, 4f, antialiased: true);
        DrawLine(end, left, SymbolColor, 4f, antialiased: true);
        DrawLine(end, right, SymbolColor, 4f, antialiased: true);
    }

    private void DrawBulwarkMark()
    {
        DrawLine(new Vector2(-18f, -12f), new Vector2(18f, -12f), SymbolColor, 5f, antialiased: true);
        DrawLine(new Vector2(-18f, 0f), new Vector2(18f, 0f), SymbolColor, 5f, antialiased: true);
        DrawLine(new Vector2(-18f, 12f), new Vector2(18f, 12f), SymbolColor, 5f, antialiased: true);
    }

    private void DrawAmplifierMark()
    {
        DrawLine(new Vector2(-16f, 0f), new Vector2(16f, 0f), SymbolColor, 5f, antialiased: true);
        DrawLine(new Vector2(0f, -16f), new Vector2(0f, 16f), SymbolColor, 5f, antialiased: true);
    }

    private void DrawPressureLabel()
    {
        var font = ThemeDB.FallbackFont;
        if (font is null)
        {
            return;
        }

        var text = _displayModel.Pressure.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var fontSize = 16;
        var size = font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        var position = new Vector2(-size.X / 2f, 28f);
        DrawString(font, position, text, HorizontalAlignment.Left, -1, fontSize, Colors.Black);
    }
}
