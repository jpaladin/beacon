using Signal.Beacon.Zigbee2Mqtt.MessageQueue;

namespace Signal.Beacon.Zigbee2Mqtt
{
    internal interface IZigbee2MqttClientFactory
    {
        IMqttClient Create();
    }
}