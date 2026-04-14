using System;
using System.Collections.Generic;

namespace Umrab.Options;

public sealed class Option<T>(string @long, IReadOnlySet<char> @short, Func<ReadOnlySpan<char>, T?, T> converter, bool isRequired = false, bool isFlag = false) : IOption {

    public string Long { get; } = @long;
    public IReadOnlySet<char> Short { get; } = @short;

    public bool IsRequired { get; } = isRequired;
    public bool IsFlag { get; } = isFlag;

    private readonly Func<ReadOnlySpan<char>, T?, T> _converter = converter;

    public Option(string @long, Func<ReadOnlySpan<char>, T?, T> converter, bool isRequired = false, bool isFlag = false) : this(@long, [], converter, isRequired, isFlag) { }
    public Option(string @long, HashSet<char> @short, Func<ReadOnlySpan<char>, T?, T> converter, bool isRequired = false, bool isFlag = false) : this(@long, (IReadOnlySet<char>)@short, converter, isRequired, isFlag) { }

    public object Convert(ReadOnlySpan<char> value, object? previous)
        => _converter(value, previous is T p ? p : default)!;
}