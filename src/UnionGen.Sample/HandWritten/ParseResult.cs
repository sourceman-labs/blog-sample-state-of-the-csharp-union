using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnionGen.Sample.HandWritten;

[Union]
public readonly struct ParseResult : IUnion
{
    [StructLayout(LayoutKind.Explicit)]
    private struct Storage
    {
        [FieldOffset(0)] public int Number;
        [FieldOffset(0)] public ErrorCode Error;
    }

    private readonly Storage _storage;
    private readonly byte _tag;

    public ParseResult(int value) { _tag = 1; _storage.Number = value; }
    public ParseResult(ErrorCode value) { _tag = 2; _storage.Error = value; }

    public object? Value => _tag switch { 1 => _storage.Number, 2 => _storage.Error, _ => null };
    public bool HasValue => _tag != 0;
    public bool TryGetValue(out int value) { value = _tag == 1 ? _storage.Number : default; return _tag == 1; }
    public bool TryGetValue(out ErrorCode value) { value = _tag == 2 ? _storage.Error : default; return _tag == 2; }
}
