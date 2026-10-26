using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace UnionGen;

internal enum Storage
{
    Reference,
    Unmanaged,
    Managed,
}

internal sealed record CaseModel(int Tag, string TypeName, string ParameterName, Storage Storage);

internal sealed record UnionModel(string? Namespace, string Name, bool IsReadOnly, EquatableArray<CaseModel> Cases);

internal readonly struct EquatableArray<T>(ImmutableArray<T> items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private ImmutableArray<T> Items => items.IsDefault ? ImmutableArray<T>.Empty : items;

    public bool Equals(EquatableArray<T> other) => Items.SequenceEqual(other.Items);
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);
    public override int GetHashCode() => Items.Aggregate(17, (hash, item) => hash * 31 + item.GetHashCode());
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
