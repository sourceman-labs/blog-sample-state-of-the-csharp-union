using Domain.After;
using UnionGen;

namespace UnionGen.Sample.Generated;

[ValueUnion, UnionCase<Ok<Shipped>>, UnionCase<Err<ShipError>>]
public readonly partial struct ShipResult;
