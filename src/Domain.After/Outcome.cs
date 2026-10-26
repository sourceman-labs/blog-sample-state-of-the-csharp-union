namespace Domain.After;

public static class Outcome
{
    public const string Shipped = "shipped";
    public const string NotPaid = "not-paid";
    public const string AlreadyShipped = "already-shipped";
    public const string Cancelled = "cancelled";
    public const string BadTrackingNumber = "bad-tracking-number";
}
