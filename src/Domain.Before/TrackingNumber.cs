using System.Diagnostics.CodeAnalysis;

namespace Domain.Before;

// "Parse, don't validate" works in C# 14 too: no public constructor, so every TrackingNumber is non-blank (except default).
public readonly record struct TrackingNumber
{
    private TrackingNumber(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse([NotNullWhen(true)] string? input, out TrackingNumber trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            trackingNumber = default;
            return false;
        }

        trackingNumber = new TrackingNumber(input.Trim());
        return true;
    }

    public override string ToString() => Value;
}
