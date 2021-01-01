using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Signal.Beacon.Core.Network;

namespace Signal.Beacon.Application.Network
{
    public class HostInfoService : IHostInfoService
    {
        public async Task<IEnumerable<IHostInfo>> HostsAsync(
            IEnumerable<string> ipAddresses,
            int[] scanPorts,
            CancellationToken cancellationToken)
        {
            var pingResults = await Task
                .WhenAll(ipAddresses.Select(address =>
                    GetHostInformationAsync(address, scanPorts, cancellationToken)))
                .ConfigureAwait(false);
            return pingResults.Where(i => i != null).Select(i => i!);
        }

        private static async Task<HostInfo?> GetHostInformationAsync(string address, IEnumerable<int> applicablePorts, CancellationToken cancellationToken)
        {
            var ping = await PingIpAddressAsync(address, cancellationToken);
            if (ping == null)
                return null;

            var portPing = Math.Min(2000, Math.Max(100, ping.Value * 2)); // Adaptive port connection timeout based on ping value
            var openPorts = (await OpenPortsAsync(address, applicablePorts, TimeSpan.FromMilliseconds(portPing))).ToList();

            return new HostInfo(address, ping.Value)
            {
                OpenPorts = openPorts
            };
        }

        private static async Task<long?> PingIpAddressAsync(string address, CancellationToken cancellationToken, int timeout = 1000, int retry = 2)
        {
            using var ping = new Ping();
            var tryCount = 0;

            while (tryCount++ < retry && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = (await ping.SendPingAsync(address, timeout).ConfigureAwait(false));
                    if (result.Status == IPStatus.Success)
                        return result.RoundtripTime;
                }
                catch
                {
                    // Do nothing
                }
            }

            return null;
        }

        private static async Task<IEnumerable<int>> OpenPortsAsync(string host, IEnumerable<int> ports, TimeSpan timeout)
        {
            var tasks = ports.Select(port => Task.Run(() =>
            {
                try
                {
                    using var client = new TcpClient();
                    var result = client.BeginConnect(host, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(timeout);
                    client.EndConnect(result);
                    return (Port: port, Open: success);
                }
                catch
                {
                    return (Port: port, Open: false);
                }
            }));

            var openPorts = await Task.WhenAll(tasks);
            return openPorts.Where(p => p.Open).Select(p => p.Port);
        }
    }
}