namespace Signal.Beacon.Core.MessageQueue
{
    public record MqttMessage(string Topic, string Payload, byte[] PayloadRaw);
}
