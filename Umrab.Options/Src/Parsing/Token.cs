using System;

namespace Umrab.Options.Parsing;

internal readonly ref struct Token {
    public readonly TokenType Type;
    public readonly ReadOnlySpan<char> Value;
    public readonly string Original;
    public readonly int Index;

    public Token(TokenType type, ReadOnlySpan<char> value, string original, int index) {
        Type = type;
        Value = value;
        Original = original;
        Index = index;
    }
}