using System.Runtime.CompilerServices;
using Domain.After;
using UnionGen.Sample;
using Generated = UnionGen.Sample.Generated;
using HandWritten = UnionGen.Sample.HandWritten;

namespace Tests;

public class UnionGenTests
{
    [Theory]
    [InlineData("42")]
    [InlineData("")]
    [InlineData("x")]
    [InlineData("-7")]
    public void Declared_hand_written_and_generated_agree(string input)
    {
        var expected = Samples.Declared(input);

        Assert.Equal(expected, Samples.HandWritten(input));
        Assert.Equal(expected, Samples.Generated(input));
    }

    public static TheoryData<HandWritten.ParseResult, Generated.ParseResult> SameValues => new()
    {
        { new HandWritten.ParseResult(42), new Generated.ParseResult(42) },
        { new HandWritten.ParseResult(0), new Generated.ParseResult(0) },
        { new HandWritten.ParseResult(-1), new Generated.ParseResult(-1) },
        { new HandWritten.ParseResult(ErrorCode.Empty), new Generated.ParseResult(ErrorCode.Empty) },
        { new HandWritten.ParseResult(ErrorCode.NotANumber), new Generated.ParseResult(ErrorCode.NotANumber) },
        { default, default },
    };

    [Theory]
    [MemberData(nameof(SameValues))]
    public void Generated_has_the_same_surface_as_hand_written(HandWritten.ParseResult hand, Generated.ParseResult gen)
    {
        Assert.Equal(hand.HasValue, gen.HasValue);
        Assert.Equal(((IUnion)hand).Value, ((IUnion)gen).Value);

        Assert.Equal(hand.TryGetValue(out int handNumber), gen.TryGetValue(out int genNumber));
        Assert.Equal(handNumber, genNumber);
        Assert.Equal(hand.TryGetValue(out ErrorCode handError), gen.TryGetValue(out ErrorCode genError));
        Assert.Equal(handError, genError);
    }

    [Fact]
    public void TryGetValue_returns_default_for_the_inactive_case()
    {
        Assert.False(new HandWritten.ParseResult(ErrorCode.NotANumber).TryGetValue(out int number));
        Assert.Equal(0, number);
        Assert.False(new HandWritten.ParseResult(7).TryGetValue(out ErrorCode error));
        Assert.Equal(default, error);
    }

    [Fact]
    public void Generated_and_hand_written_have_the_same_size()
    {
        Assert.Equal(Unsafe.SizeOf<HandWritten.ParseResult>(), Unsafe.SizeOf<Generated.ParseResult>());
    }

    [Theory]
    [InlineData("42", true)]
    [InlineData("", false)]
    public void Generated_converts_and_matches_like_hand_written(string input, bool isNumber)
    {
        HandWritten.ParseResult hand = isNumber ? int.Parse(input) : ErrorCode.Empty;
        Generated.ParseResult gen = isNumber ? int.Parse(input) : ErrorCode.Empty;

        Assert.Equal(hand is int, gen is int);
        Assert.Equal(hand is ErrorCode.Empty, gen is ErrorCode.Empty);
        Assert.Equal(isNumber ? 42 : -1, gen.Match(static n => n, static e => -(int)e));
    }

    [Fact]
    public void Value_boxes_the_active_case()
    {
        Assert.Equal(42L, ((IUnion)new Generated.JsonScalar(42L)).Value);
        Assert.Equal(true, ((IUnion)new Generated.JsonScalar(true)).Value);
        Assert.Equal(ErrorCode.Empty, ((IUnion)new Generated.ParseResult(ErrorCode.Empty)).Value);
    }

    [Fact]
    public void Nested_patterns_work_on_generated_unions()
    {
        Generated.ShipResult result = Result.Err<ShipError>(new BadTrackingNumber("nope"));

        var outcome = result switch
        {
            Ok<Shipped> => "ok",
            Err<ShipError>(BadTrackingNumber b) => b.Input,
            Err<ShipError>(NotPaid or AlreadyShipped or OrderCancelled) => "other",
        };

        Assert.Equal("nope", outcome);
    }

    [Fact]
    public void Default_holds_no_value()
    {
        var scalar = default(Generated.JsonScalar);

        Assert.False(scalar.HasValue);
        Assert.Null(((IUnion)scalar).Value);
        Assert.Throws<InvalidOperationException>(() => scalar.Match(static s => 1, static l => 2, static d => 3, static b => 4));
    }

    [Fact]
    public void Reference_cases_reject_null()
    {
        Assert.Throws<ArgumentNullException>(() => new Generated.JsonScalar((string)null!));
    }
}
