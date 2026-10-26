using UnionGen;

namespace UnionGen.Sample.Generated;

[ValueUnion, UnionCase<string>, UnionCase<long>, UnionCase<double>, UnionCase<bool>]
public readonly partial struct JsonScalar;
