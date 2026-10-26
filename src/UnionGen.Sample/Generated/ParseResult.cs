using UnionGen;

namespace UnionGen.Sample.Generated;

[ValueUnion, UnionCase<int>, UnionCase<ErrorCode>]
public readonly partial struct ParseResult;
