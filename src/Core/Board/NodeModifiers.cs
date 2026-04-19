namespace PressureChain.Core.Board;

[Flags]
public enum NodeModifiers
{
    None = 0,
    Frozen = 1 << 0,
    Insulated = 1 << 1
}
