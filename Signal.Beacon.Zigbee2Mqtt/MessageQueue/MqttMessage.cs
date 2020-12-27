namespace Signal.Beacon.Zigbee2Mqtt.MessageQueue
{
    public record MqttMessage(string Topic, string Payload, byte[] PayloadRaw);
}
