using System;
using System.Collections.Generic;

using Umrab.Options.Parsing;

namespace Umrab.Options;

public sealed class Command(string name, IReadOnlySet<char> @short) {
    public string Name { get; init; } = name;
    public IReadOnlySet<char> Short { get; } = @short;

    private readonly Dictionary<string, Command> _longCommands = new(StringComparer.Ordinal);
    private readonly Dictionary<char, Command> _shortCommand = [];
    private readonly Dictionary<string, IOption> _longOptions = new(StringComparer.Ordinal);
    private readonly Dictionary<char, IOption> _shortOptions = [];
    private readonly List<IArgument> _arguments = [];

    public Command() : this("", []) { }
    public Command(string name) : this(name, []) { }
    public Command(string name, HashSet<char> @short) : this(name, (IReadOnlySet<char>)@short) { }

    public Command Add<T>(Option<T> option) {
        if (_longOptions.ContainsKey(option.Long)) {
            throw new ArgumentException($"Option with long name '{option.Long}' already exists.", nameof(option));
        }
        foreach (char c in option.Short) {
            if (_shortOptions.ContainsKey(c)) {
                throw new ArgumentException($"Option with short name '{c}' already exists.", nameof(option));
            }
        }

        _longOptions.Add(option.Long, option);
        foreach (char c in option.Short) {
            _shortOptions.Add(c, option);
        }
        return this;
    }

    public Command Add<T>(Argument<T> argument) {
        _arguments.Add(argument);
        return this;
    }

    public Command Add(Command command) {
        if (_longCommands.ContainsKey(command.Name)) {
            throw new ArgumentException($"Subcommand with name '{command.Name}' already exists.", nameof(command));
        }
        foreach (char c in command.Short) {
            if (_shortCommand.ContainsKey(c)) {
                throw new ArgumentException($"Subcommand with short name '{c}' already exists.", nameof(command));
            }
        }

        _longCommands.Add(command.Name, command);
        foreach (char c in command.Short) {
            _shortCommand.Add(c, command);
        }
        return this;
    }

    public ParseResult Parse(string[] args, bool matchSelf = false) {
        ArrayTokenizer tokenizer = new(args);
        return Parse(ref tokenizer, matchSelf);
    }

    private ParseResult Parse(ref ArrayTokenizer tokenizer, bool matchSelf = false) {
        Dictionary<IOption, object> options = new(_longOptions.Count);
        Dictionary<IArgument, object> arguments = new(_arguments.Count);

        IOption? option = null;

        if (matchSelf) {
            if (!tokenizer.Next(out Token token)) throw new InvalidOperationException($"Command '{Name}' is required.");

            if (token.Type != TokenType.ArgumentOrValue
             || !(token.Value.Equals(Name.AsSpan(), StringComparison.Ordinal)
             || (token.Value.Length == 1 && Short.Contains(token.Value[0])))) {
                throw new InvalidOperationException($"Expected command '{Name}' in '{token.Original}' at index {token.Index}.");
            }
        }

        while (tokenizer.Next(out Token token)) {
            Command? command = ProcessToken(token, tokenizer.EndOfOptions, options, arguments, ref option);
            if (command != null) {
                ValidateRequiredElements(options, arguments.Count);
                return new ParseResult(this, command.Parse(ref tokenizer), options, arguments);
            }
        }

        if (option != null) throw new InvalidOperationException($"Option '{option.Long}' expects a value.");

        ValidateRequiredElements(options, arguments.Count);
        return new ParseResult(this, null, options, arguments);
    }

    public ParseResult Parse(string command, bool matchSelf = false) {
        StringTokenizer tokenizer = new(command);
        return Parse(ref tokenizer, matchSelf);
    }

    private ParseResult Parse(ref StringTokenizer tokenizer, bool matchSelf = false) {
        Dictionary<IOption, object> options = new(_longOptions.Count);
        Dictionary<IArgument, object> arguments = new(_arguments.Count);

        IOption? option = null;

        if (matchSelf) {
            if (!tokenizer.Next(out Token token)) throw new InvalidOperationException($"Command '{Name}' is required.");

            if (token.Type != TokenType.ArgumentOrValue
             || !(token.Value.Equals(Name.AsSpan(), StringComparison.Ordinal)
             || (token.Value.Length == 1 && Short.Contains(token.Value[0])))) {
                throw new InvalidOperationException($"Expected command '{Name}' in '{token.Original}' at index {token.Index}.");
            }
        }

        while (tokenizer.Next(out Token token)) {
            Command? command = ProcessToken(token, tokenizer.EndOfOptions, options, arguments, ref option);
            if (command != null) {
                ValidateRequiredElements(options, arguments.Count);
                return new ParseResult(this, command.Parse(ref tokenizer), options, arguments);
            }
        }

        if (option != null) throw new InvalidOperationException($"Option '{option.Long}' expects a value.");

        ValidateRequiredElements(options, arguments.Count);
        return new ParseResult(this, null, options, arguments);
    }

    private Command? ProcessToken(Token token, bool endOfOptions, Dictionary<IOption, object> options, Dictionary<IArgument, object> arguments, ref IOption? current) {
        if (current is not null) {
            if (token.Type == TokenType.ArgumentOrValue) {
                object? prev = options.TryGetValue(current, out object? p) ? p : default;
                options[current] = current.Convert(token.Value, prev);
                current = null;
                return null;
            }

            throw new InvalidOperationException($"Option '{current.Long}' expects a value.");
        }

        if (token.Type == TokenType.LongKey || token.Type == TokenType.ShortKey) {
            IOption? o;
            if (token.Type == TokenType.LongKey) {
                _longOptions.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(token.Value, out o);
            } else {
                _shortOptions.TryGetValue(token.Value[0], out o);
            }

            if (o == null) {
                throw new InvalidOperationException($"Unknown option in '{token.Original}' at index {token.Index}.");
            }

            if (o.IsFlag) {
                object? prev = options.TryGetValue(o, out object? p) ? p : default;
                options[o] = o.Convert([], prev);
            } else {
                current = o;
            }

            return null;
        }

        if (token.Type == TokenType.ArgumentOrValue) {
            if (!endOfOptions && _longCommands.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(token.Value, out Command? command)) {
                return command;
            }

            if (!endOfOptions && token.Value.Length == 1 && _shortCommand.TryGetValue(token.Value[0], out command)) {
                return command;
            }

            if (arguments.Count < _arguments.Count) {
                IArgument argument = _arguments[arguments.Count];
                arguments[argument] = argument.Convert(token.Value);
                return null;
            }

            throw new InvalidOperationException($"Unknown option in '{token.Original}' at index {token.Index}.");
        }

        return null;
    }

    private void ValidateRequiredElements(Dictionary<IOption, object> parsedOptions, int parsedArgumentCount) {
        foreach (IOption option in _longOptions.Values) {
            if (option.IsRequired && !parsedOptions.ContainsKey(option)) {
                throw new InvalidOperationException($"Required option '{option.Long}' was not provided.");
            }
        }

        if (parsedArgumentCount < _arguments.Count) {
            for (int i = parsedArgumentCount; i < _arguments.Count; i++) {
                if (_arguments[i].IsRequired) {
                    throw new InvalidOperationException($"Required argument at index {i} was not provided.");
                }
            }
        }
    }
}