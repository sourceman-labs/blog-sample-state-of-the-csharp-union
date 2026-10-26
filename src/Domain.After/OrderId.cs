namespace Domain.After;

// One type for an order id everywhere: an OrderId cannot be mixed up with a customer id or a quantity.
public readonly record struct OrderId(int Value)
{
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
