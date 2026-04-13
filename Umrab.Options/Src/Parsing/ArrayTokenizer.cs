using System;

namespace Umrab.Options.Parsing;

internal ref struct ArrayTokenizer(ReadOnlySpan<string> arguments) {
    private readonly ReadOnlySpan<string> _arguments = arguments;
    private int _index = 0;
    private int _charIndex = 0;
    public bool EndOfOptions { get; private set; } = false;

    public bool Next(out Token token) {
    start:

        if (_index >= _arguments.Length) {
            token = default;
            return false;
        }

        string argument = _arguments[_index];

        if (_charIndex > 0) {
            token = new Token(TokenType.ShortKey, argument.AsSpan(_charIndex, 1), argument, _charIndex);

            _charIndex++;
            if (_charIndex >= argument.Length) {
                _index++;
                _charIndex = 0;
            }
            return true;
        }

        if (EndOfOptions) {
            token = new Token(TokenType.ArgumentOrValue, argument, argument, 0);
            _index++;
            return true;
        }

        if (argument.StartsWith("--", StringComparison.Ordinal)) {
            if (argument.Length == 2) {
                EndOfOptions = true;
                _index++;
                goto start;
            }

            token = new Token(TokenType.LongKey, argument.AsSpan(2), argument, 0);
            _index++;
            return true;
        }

        if (argument.StartsWith('-')) {
            if (argument.Length == 1) {
                token = new Token(TokenType.ArgumentOrValue, argument, argument, 0);
                _index++;
                return true;
            }

            _charIndex = 1;
            goto start;
        }

        token = new Token(TokenType.ArgumentOrValue, argument, argument, 0);
        _index++;
        return true;
    }
}