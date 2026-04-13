using System;

namespace Umrab.Options;

public sealed class Argument<T>(bool isRequired, Func<ReadOnlySpan<char>, T> converter) : IArgument {
    public bool IsRequired { get; init; } = isRequired;

    private readonly Func<ReadOnlySpan<char>, T> _converter = converter;

    public object Convert(ReadOnlySpan<char> value) => _converter(value)!;
}