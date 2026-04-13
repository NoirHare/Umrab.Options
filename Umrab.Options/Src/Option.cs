using System;
using System.Collections.Generic;

namespace Umrab.Options;

public sealed class Option<T>(string @long, IReadOnlySet<char> @short, bool isRequired, bool isFlag, Func<ReadOnlySpan<char>, T?, T> converter) : IOption {

    public string Long { get; } = @long;
    public IReadOnlySet<char> Short { get; } = @short;

    public bool IsRequired { get; } = isRequired;
    public bool IsFlag { get; } = isFlag;

    private readonly Func<ReadOnlySpan<char>, T?, T> _converter = converter;

    public Option(string @long, bool isRequired, bool isFlag, Func<ReadOnlySpan<char>, T?, T> converter) : this(@long, [], isRequired, isFlag, converter) { }
    public Option(string @long, HashSet<char> @short, bool isRequired, bool isFlag, Func<ReadOnlySpan<char>, T?, T> converter) : this(@long, (IReadOnlySet<char>)@short, isRequired, isFlag, converter) { }

    public object Convert(ReadOnlySpan<char> value, object? previous)
        => _converter(value, previous is T p ? p : default)!;
}