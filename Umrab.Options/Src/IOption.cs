using System;
using System.Collections.Generic;

namespace Umrab.Options;

internal interface IOption {
    string Long { get; }
    IReadOnlySet<char> Short { get; }

    bool IsRequired { get; }
    bool IsFlag { get; }

    object Convert(ReadOnlySpan<char> value, object? previous);
}
