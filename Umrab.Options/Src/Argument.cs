using System;

namespace Umrab.Options;

public sealed class Argument<T>(Func<ReadOnlySpan<char>, T> converter, bool isRequired = true) : IArgument {
    public bool IsRequired { get; init; } = isRequired;

    private readonly Func<ReadOnlySpan<char>, T> _converter = converter;

    public object Convert(ReadOnlySpan<char> value) => _converter(value)!;
}