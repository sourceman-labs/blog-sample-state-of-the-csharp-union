namespace Domain.After;

public readonly record struct PaymentId(string Value)
{
    public override string ToString() => Value;
}
