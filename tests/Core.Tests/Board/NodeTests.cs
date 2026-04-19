using PressureChain.Core.Board;

namespace PressureChain.Core.Tests.Board;

public sealed class NodeTests
{
    [Fact]
    public void State_CellWithPressure50_IsCritical()
    {
        var node = new Node(
            NodeType.Cell,
            Pressure: 50,
            Facing: null,
            Connections: ConnectionMask.AllOpen(),
            Modifiers: NodeModifiers.None);

        Assert.Equal(NodeState.Critical, node.State);
    }
}
