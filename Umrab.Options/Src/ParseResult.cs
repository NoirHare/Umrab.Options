using System;
using System.Collections.Generic;

namespace Umrab.Options;

public sealed class ParseResult {
    public Command Command { get; }
    public ParseResult? SubResult { get; }

    private readonly IReadOnlyDictionary<IOption, object> _options;
    private readonly IReadOnlyDictionary<IArgument, object> _arguments;

    internal ParseResult(Command command, ParseResult? subResult, IReadOnlyDictionary<IOption, object> options, IReadOnlyDictionary<IArgument, object> arguments) {
        Command = command;
        SubResult = subResult;
        _options = options;
        _arguments = arguments;
    }

    public T GetValue<T>(Option<T> option, Func<T> factory)
        => _options.TryGetValue(option, out object? value) ? (T)value : factory();

    public T GetValue<T>(Argument<T> argument, Func<T> factory)
        => _arguments.TryGetValue(argument, out object? value) ? (T)value : factory();

    public T GetRequiredValue<T>(Option<T> option) {
        if (_options.TryGetValue(option, out object? value)) return (T)value;

        throw new InvalidOperationException($"Required option '{option.Long}' was not found in the result.");
    }

    public T GetRequiredValue<T>(Argument<T> argument) {
        if (_arguments.TryGetValue(argument, out object? value)) return (T)value;

        throw new InvalidOperationException($"Required argument was not found in the result.");
    }
}