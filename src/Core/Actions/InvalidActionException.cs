namespace PressureChain.Core.Actions;

public sealed class InvalidActionException : Exception
{
    public InvalidActionException(string reason)
        : base(reason)
    {
        Reason = reason;
    }

    public string Reason { get; }
}
