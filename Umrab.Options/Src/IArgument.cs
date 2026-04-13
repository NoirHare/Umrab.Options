using System;

namespace Umrab.Options;

internal interface IArgument {
    bool IsRequired { get; }
    
    object Convert(ReadOnlySpan<char> value);
}
