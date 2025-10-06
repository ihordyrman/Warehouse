namespace Warehouse.Core.Shared.Domain;

public record Pair(Instrument Left, Instrument Right)
{
    public override string ToString() => $"{Left.ToString()}-{Right.ToString()}";

    public static implicit operator string(Pair p) => $"{p.Left.ToString()}-{p.Right.ToString()}";
}
