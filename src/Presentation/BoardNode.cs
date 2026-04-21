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
    private IReadOnlyList<HexCoord> _objectiveCoords = Array.Empty<HexCoord>();
    private IReadOnlyList<HexCoord> _clearedCoords = Array.Empty<HexCoord>();
    private HexCoord? _selectedCoord;
    private bool _inputEnabled = true;

    public event Action<PlayerAction>? ActionRequested;

    public void DisplayBoard(GameBoard board, IReadOnlyList<HexCoord> objectiveCoords, IReadOnlyList<HexCoord> clearedCoords)
    {
        _board = board ?? throw new ArgumentNullException(nameof(board));
        _objectiveCoords = objectiveCoords ?? Array.Empty<HexCoord>();
        _clearedCoords = clearedCoords ?? Array.Empty<HexCoord>();
        EnsureCells(board);

        foreach (var coord in board.Coords)
        {
            var cell = _cells[coord];
            cell.Bind(coord, board.NodeAt(coord));
            cell.SetObjectiveProgress(_objectiveCoords.Contains(coord), _clearedCoords.Contains(coord));
        }

        RefreshVisualHints();
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
            ActionRequested?.Invoke(new TriggerEarlyAction(coord));
            return;
        }

        if (_selectedCoord is null)
        {
            SetSelection(coord);
            return;
        }

        if (_selectedCoord.Value == coord)
        {
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
            ActionRequested?.Invoke(new MergeAction(selectedCoord, coord));
            return;
        }

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
        RefreshVisualHints();
    }

    private void ClearSelection()
    {
        _selectedCoord = null;
        RefreshVisualHints();
    }

    private void RefreshVisualHints()
    {
        if (_board is null)
        {
            return;
        }

        BoardCell? selectedNode = _selectedCoord is null ? null : _board.NodeAt(_selectedCoord.Value);
        foreach (var (coord, cell) in _cells)
        {
            cell.SetSelected(_selectedCoord == coord);
            cell.SetObjectiveProgress(_objectiveCoords.Contains(coord), _clearedCoords.Contains(coord));

            var node = _board.NodeAt(coord);
            var isMergeCandidate =
                _selectedCoord is not null &&
                _selectedCoord.Value != coord &&
                selectedNode is not null &&
                _selectedCoord.Value.DistanceTo(coord) == 1 &&
                selectedNode?.Type == node.Type &&
                node.Type != NodeType.Bulwark;
            var isTriggerEligible = node.Type != NodeType.Bulwark && node.Pressure >= 50;

            cell.SetInteractionHints(isMergeCandidate, isTriggerEligible);
        }
    }
}
