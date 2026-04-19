using PressureChain.Core.Board;

namespace PressureChain.Core.Tests.Board;

public sealed class NodeStateRulesTests
{
    [Theory]
    [InlineData(0, NodeState.Stable)]
    [InlineData(24, NodeState.Stable)]
    [InlineData(25, NodeState.Swelling)]
    [InlineData(49, NodeState.Swelling)]
    [InlineData(50, NodeState.Critical)]
    [InlineData(74, NodeState.Critical)]
    [InlineData(75, NodeState.Volatile)]
    [InlineData(99, NodeState.Volatile)]
    [InlineData(100, NodeState.Burst)]
    public void FromPressure_MapsToExpectedThresholdState(int pressure, NodeState expectedState)
    {
        var actualState = NodeStateRules.FromPressure(pressure);

        Assert.Equal(expectedState, actualState);
    }
}
