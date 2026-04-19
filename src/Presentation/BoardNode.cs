using Godot;
using PressureChain.Core.Actions;
using PressureChain.Core.Board;
using PressureChain.Core.Chains;
using PressureChain.Core.Grid;
using GameBoard = PressureChain.Core.Board.Board;
using BoardCell = PressureChain.Core.Board.Node;

namespace PressureChain.Presentation;

public partial class BoardNode : Node2D
{
    private readonly Dictionary<HexCoord, HexCellNode> _cells = [];
    private GameBoard? _board;
    private HexCoord? _selectedCoord;
    private bool _inputEnabled = true;

    public event Action<PlayerAction>? ActionRequested;
    public event Action<string>? DebugEventRaised;

    public void DisplayBoard(GameBoard board)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
        EnsureCells(board);

        foreach (var coord in board.Coords)
        {
            var cell = _cells[coord];
            cell.Bind(coord, board.NodeAt(coord));
            cell.SetSelected(_selectedCoord == coord);
        }
    }

    public void SetInputEnabled(bool inputEnabled)
    {
        _inputEnabled = inputEnabled;
        if (!inputEnabled)
        {
            ClearSelection();
        }
    }

    public async System.Threading.Tasks.Task PlayChainResolutionAsync(ChainResolution resolution)
    {
        foreach (var wave in resolution.Waves)
        {
            foreach (var burst in wave)
            {
                if (_cells.TryGetValue(burst.Origin, out var cell))
                {
                    _ = cell.PlayBurstAnimationAsync();
                }
            }

            await ToSignal(GetTree().CreateTimer(0.15d), SceneTreeTimer.SignalName.Timeout);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_inputEnabled || _board is null || @event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        var targetCoord = FindCoordAtPoint(ToLocal(mouseButton.GlobalPosition));
        if (targetCoord is null)
        {
            return;
        }

        var coord = targetCoord.Value;
        var targetNode = _board.NodeAt(coord);

        switch (mouseButton.ButtonIndex)
        {
            case MouseButton.Left:
                HandleLeftClick(coord, targetNode, mouseButton.ShiftPressed);
                break;
            case MouseButton.Right:
                HandleRightClick(coord, targetNode);
                break;
        }
    }

    private void EnsureCells(GameBoard board)
    {
        if (_cells.Count == board.Coords.Count && board.Coords.All(_cells.ContainsKey))
        {
            UpdateLayout(board);
            return;
        }

        foreach (var existing in _cells.Values)
        {
            existing.QueueFree();
        }

        _cells.Clear();
        foreach (var coord in board.Coords)
        {
            var cell = new HexCellNode
            {
                Name = $"Hex_{coord.Q}_{coord.R}"
            };

            AddChild(cell);
            _cells.Add(coord, cell);
        }

        UpdateLayout(board);
    }

    private void UpdateLayout(GameBoard board)
    {
        var positions = board.Coords.ToDictionary(coord => coord, coord => HexLayout.CoordToPixel(coord, 48f));
        var minX = positions.Values.Min(position => position.X);
        var maxX = positions.Values.Max(position => position.X);
        var minY = positions.Values.Min(position => position.Y);
        var maxY = positions.Values.Max(position => position.Y);
        var offset = new Vector2(-(minX + maxX) / 2f, -(minY + maxY) / 2f);

        foreach (var (coord, cell) in _cells)
        {
            cell.Position = positions[coord] + offset;
        }
    }

    private void HandleLeftClick(HexCoord coord, BoardCell targetNode, bool shiftPressed)
    {
        if (shiftPressed && targetNode.Type != NodeType.Bulwark && targetNode.Pressure >= 50)
        {
            ClearSelection();
            DebugEventRaised?.Invoke($"Queued {DebugEventFormatter.FormatAction(new TriggerEarlyAction(coord))} on {DebugEventFormatter.FormatNode(coord, targetNode)}");
            ActionRequested?.Invoke(new TriggerEarlyAction(coord));
            return;
        }

        if (_selectedCoord is null)
        {
            DebugEventRaised?.Invoke($"Selected {DebugEventFormatter.FormatNode(coord, targetNode)}");
            SetSelection(coord);
            return;
        }

        if (_selectedCoord.Value == coord)
        {
            DebugEventRaised?.Invoke($"Cleared selection at {DebugEventFormatter.FormatNode(coord, targetNode)}");
            ClearSelection();
            return;
        }

        var selectedCoord = _selectedCoord.Value;
        var selectedNode = _board!.NodeAt(selectedCoord);
        if (selectedCoord.DistanceTo(coord) == 1 &&
            selectedNode.Type == targetNode.Type &&
            targetNode.Type != NodeType.Bulwark)
        {
            ClearSelection();
            DebugEventRaised?.Invoke(
                $"Queued {DebugEventFormatter.FormatAction(new MergeAction(selectedCoord, coord))} | A {DebugEventFormatter.FormatNode(selectedCoord, selectedNode)} | B {DebugEventFormatter.FormatNode(coord, targetNode)}");
            ActionRequested?.Invoke(new MergeAction(selectedCoord, coord));
            return;
        }

        DebugEventRaised?.Invoke($"Changed selection to {DebugEventFormatter.FormatNode(coord, targetNode)}");
        SetSelection(coord);
    }

    private void HandleRightClick(HexCoord coord, BoardCell targetNode)
    {
        if (targetNode.Type != NodeType.Vent || targetNode.Facing is null)
        {
            return;
        }

        ClearSelection();
        var action = new VentRedirectAction(coord, targetNode.Facing.Value.RotateClockwise());
        DebugEventRaised?.Invoke($"Queued {DebugEventFormatter.FormatAction(action)} on {DebugEventFormatter.FormatNode(coord, targetNode)}");
        ActionRequested?.Invoke(action);
    }

    private HexCoord? FindCoordAtPoint(Vector2 localPoint)
    {
        foreach (var (coord, cell) in _cells)
        {
            var pointInCell = localPoint - cell.Position;
            if (cell.ContainsLocalPoint(pointInCell))
            {
                return coord;
            }
        }

        return null;
    }

    private void SetSelection(HexCoord coord)
    {
        _selectedCoord = coord;
        RefreshSelection();
    }

    private void ClearSelection()
    {
        _selectedCoord = null;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var (coord, cell) in _cells)
        {
            cell.SetSelected(_selectedCoord == coord);
        }
    }
}
