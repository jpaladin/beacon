using System.Text.Json;
using Signal.Beacon.Core.Signal;

namespace Signal.Beacon.Application.Signal
{
    internal static class SignalFeatureClientExtensions
    {
        public static string? SerializeValue(this ISignalFeatureClient _, object? value) =>
            value switch
            {
                null => null,
                string stringValue => stringValue,
                _ => JsonSerializer.Serialize(value)
            };
    }
}