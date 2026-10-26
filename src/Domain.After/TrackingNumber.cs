using System.Diagnostics.CodeAnalysis;

namespace Domain.After;

// No public constructor: Parse is the only way in, so every TrackingNumber is non-blank (except default).
public readonly record struct TrackingNumber
{
    private TrackingNumber(string value) => Value = value;

    public string Value { get; }

    public static Result<TrackingNumber, BadTrackingNumber> Parse(string input) =>
        TryParse(input, out var trackingNumber) ? Result.Ok(trackingNumber) : Result.Err(new BadTrackingNumber(input));

    // The non-boxing form, for hot paths.
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
