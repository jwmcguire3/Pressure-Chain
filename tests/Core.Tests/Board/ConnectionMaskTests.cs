using PressureChain.Core.Board;
using PressureChain.Core.Grid;

namespace PressureChain.Core.Tests.Board;

public sealed class ConnectionMaskTests
{
    [Fact]
    public void DefaultMask_HasAllDirectionsOpen()
    {
        var mask = new ConnectionMask();

        foreach (var direction in Enum.GetValues<HexDirection>())
        {
            Assert.True(mask.IsOpen(direction));
        }
    }

    [Fact]
    public void With_ClosingOneDirection_DoesNotAffectOtherDirections()
    {
        var mask = ConnectionMask.AllOpen();

        var updated = mask.With(HexDirection.NE, open: false);

        Assert.False(updated.IsOpen(HexDirection.NE));
        foreach (var direction in Enum.GetValues<HexDirection>().Where(d => d != HexDirection.NE))
        {
            Assert.True(updated.IsOpen(direction));
        }
    }

    [Fact]
    public void With_ReopeningDirection_RestoresOpenState()
    {
        var closed = ConnectionMask.AllOpen().With(HexDirection.SW, open: false);

        var reopened = closed.With(HexDirection.SW, open: true);

        Assert.True(reopened.IsOpen(HexDirection.SW));
    }
}
