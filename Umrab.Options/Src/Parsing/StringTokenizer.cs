using System;

namespace Umrab.Options.Parsing;

internal ref struct StringTokenizer(string command) {
    private readonly string _command = command;

    private ReadOnlySpan<char> _argument;

    private int _offset = 0;
    private int _argumentOffset = 0;
    private int _charIndex;
    public bool EndOfOptions { get; private set; } = false;

    public bool Next(out Token token) {
    start:

        if (_charIndex > 0) {
            token = new Token(
                TokenType.ShortKey,
                _argument.Slice(_charIndex, 1),
                _command,
                _argumentOffset + _charIndex
            );

            _charIndex++;
            if (_charIndex >= _argument.Length) {
                _charIndex = 0;
            }
            return true;
        }

        if (!NextArgument(out _argument, out _argumentOffset)) {
            token = default;
            return false;
        }

        if (EndOfOptions) {
            token = new Token(TokenType.ArgumentOrValue, _argument, _command, _argumentOffset);
            return true;
        }

        if (_argument.StartsWith("--", StringComparison.Ordinal)) {
            if (_argument.Length == 2) {
                EndOfOptions = true;
                goto start;
            }

            token = new Token(TokenType.LongKey, _argument[2..], _command, _argumentOffset);
            return true;
        }

        if (_argument.StartsWith('-')) {
            if (_argument.Length == 1) {
                token = new Token(TokenType.ArgumentOrValue, _argument, _command, _argumentOffset);
                return true;
            }

            _charIndex = 1;
            goto start;
        }

        token = new Token(TokenType.ArgumentOrValue, _argument, _command, _argumentOffset);
        return true;
    }

    private bool NextArgument(out ReadOnlySpan<char> argument, out int offset) {
        while (_offset < _command.Length && char.IsWhiteSpace(_command[_offset])) _offset++;

        if (_offset >= _command.Length) {
            argument = default; offset = default;
            return false;
        }

        int start = _offset;
        bool quoted = false;
        char quote = '\0';

        while (_offset < _command.Length) {
            char c = _command[_offset];

            if (c == '"' || c == '\'') {
                if (quoted && c == quote) {
                    quoted = false;
                } else if (!quoted) {
                    quoted = true;
                    quote = c;
                }
            } else if (!quoted && char.IsWhiteSpace(c)) {
                break;
            }

            _offset++;
        }

        if (quoted) throw new InvalidOperationException($"Unterminated quoted argument at index {start}.");

        int length = _offset - start;

        argument = _command.AsSpan(start, length);
        offset = start;

        if (length >= 2) {
            char first = argument[0];
            char last = argument[length - 1];
            if ((first == '"' || first == '\'') && first == last) {
                argument = argument.Slice(1, length - 2);
                offset++;
            }
        }

        return true;
    }
}