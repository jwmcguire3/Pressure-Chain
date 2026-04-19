using PressureChain.Core.Grid;

namespace PressureChain.Core.Board;

public readonly record struct Node(
    NodeType Type,
    int Pressure,
    HexDirection? Facing,
    ConnectionMask Connections,
    NodeModifiers Modifiers)
{
    public NodeState State => NodeStateRules.FromPressure(Pressure);
}
