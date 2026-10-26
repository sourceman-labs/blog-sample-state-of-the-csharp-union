#if DEMO_CS0457
namespace Domain.After.Pitfalls;

public union NaiveResult<T, E>(T, E);

public static class TypeUnionPitfall
{
    public static NaiveResult<string, string> Parse(string input) => input;
}
#endif
