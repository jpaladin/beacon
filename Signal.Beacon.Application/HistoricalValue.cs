using System;
using Signal.Beacon.Core.Values;

namespace Signal.Beacon.Application
{
    public record HistoricalValue(object? Value, DateTime TimeStamp) : IHistoricalValue;
}