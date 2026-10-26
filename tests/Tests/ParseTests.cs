using Domain.After;

namespace Tests;

public class ParseTests
{
    [Theory]
    [InlineData("TRACK-1")]
    [InlineData(" TRACK-1 ")]
    [InlineData("\tTRACK-1\n")]
    public void TrackingNumber_trims_non_blank_input(string input)
    {
        Assert.True(TrackingNumber.Parse(input).IsOk(out var trackingNumber, out _));
        Assert.Equal("TRACK-1", trackingNumber.Value);
        Assert.Equal("TRACK-1", trackingNumber.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public void TrackingNumber_rejects_blank_input_and_keeps_it(string? input)
    {
        Assert.False(TrackingNumber.Parse(input!).IsOk(out _, out var error));
        Assert.Equal(new BadTrackingNumber(input!), error);
    }

    [Fact]
    public void Parse_results_convert_to_the_wider_error_union()
    {
        Assert.False(TrackingNumber.Parse("").IsOk(out _, out var bad));
        ShipError error = bad!;

        Assert.Equal("bad-tracking-number", error switch
        {
            NotPaid => "not-paid",
            AlreadyShipped => "already-shipped",
            OrderCancelled => "cancelled",
            BadTrackingNumber => "bad-tracking-number",
        });
    }
}
