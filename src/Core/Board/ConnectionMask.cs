using PressureChain.Core.Grid;

namespace PressureChain.Core.Board;

public readonly record struct ConnectionMask(byte Bits)
{
    private const byte ValidBitsMask = 0b0011_1111;

    public ConnectionMask() : this(ValidBitsMask)
    {
    }

    public static ConnectionMask AllOpen() => new(ValidBitsMask);

    public static ConnectionMask AllClosed() => new(0);

    public bool IsOpen(HexDirection direction)
    {
        var bit = DirectionToBit(direction);
        return (Bits & bit) != 0;
    }

    public ConnectionMask With(HexDirection direction, bool open)
    {
        var bit = DirectionToBit(direction);
        var updatedBits = open ? (byte)(Bits | bit) : (byte)(Bits & ~bit);
        return new ConnectionMask((byte)(updatedBits & ValidBitsMask));
    }

    private static byte DirectionToBit(HexDirection direction) => direction switch
    {
        HexDirection.E => 1 << 0,
        HexDirection.NE => 1 << 1,
        HexDirection.NW => 1 << 2,
        HexDirection.W => 1 << 3,
        HexDirection.SW => 1 << 4,
        HexDirection.SE => 1 << 5,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };
}
