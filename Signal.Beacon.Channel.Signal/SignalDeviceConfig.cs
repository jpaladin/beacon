using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Signal.Beacon.Channel.Signal
{
    public class SignalDeviceConfig
    {
        [JsonPropertyName("api")]
        public string Api { get; set; }

        [JsonPropertyName("v")]
        public string Version { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("epoch")]
        public int Epoch { get; set; }

        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("wifiAlive")]
        public bool WifiAlive { get; set; }

        [JsonPropertyName("ssid")]
        public string Ssid { get; set; }

        [JsonPropertyName("hostname")]
        public string Hostname { get; set; }

        [JsonPropertyName("mqttAlive")]
        public bool MqttAlive { get; set; }

        [JsonPropertyName("mqttAddress")]
        public string MqttAddress { get; set; }

        [JsonPropertyName("mqttTopic")]
        public string MqttTopic { get; set; }

        [JsonPropertyName("endpoints")]
        public DeviceEndpoints Endpoints { get; set; }

        public class DeviceEndpoints
        {
            [JsonPropertyName("inputs")]
            public List<Input> Inputs { get; set; }

            [JsonPropertyName("outputs")]
            public List<Output> Outputs { get; set; }

            public class Input
            {
                [JsonPropertyName("dataType")]
                public string DataType { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }

                [JsonPropertyName("value")]
                public bool Value { get; set; }
            }

            public class Output
            {
                [JsonPropertyName("dataType")]
                public string DataType { get; set; }

                [JsonPropertyName("name")]
                public string Name { get; set; }
            }
        }
    }
}