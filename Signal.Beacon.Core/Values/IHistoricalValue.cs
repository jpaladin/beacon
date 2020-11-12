using System;

namespace Signal.Beacon.Core.Values
{
    public interface IHistoricalValue
    {
        object? Value { get; }

        DateTime TimeStamp { get; }
    }
}
