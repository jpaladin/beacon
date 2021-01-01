using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Signal.Beacon.Core.Network
{
    // ReSharper disable once InconsistentNaming
    public static class IPHelper
    {
        private const string FallbackLocalIpAddress = "127.0.0.1";

        public static string GetLocalIp()
        {
            try
            {
                using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("1.1.1.1", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? FallbackLocalIpAddress;
            }
            catch
            {
                return FallbackLocalIpAddress;
            }
        }

        public static IEnumerable<string> GetIPAddressesInRange(string ipAddress, int netMask = 24)
        {
            if (netMask != 24)
                throw new NotImplementedException($"Specified net mask (/{netMask}) not implemented.");

            var localIpPrefix = ipAddress.Substring(0, ipAddress.LastIndexOf('.') + 1);
            return Enumerable.Range(0, 256).Select(i => $"{localIpPrefix}{i}");
        }
    }
}