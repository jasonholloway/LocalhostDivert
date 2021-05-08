using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WinDivertSharp;

namespace MiniNAT
{
    internal class Program
    {
        static Task Main(string[] args)
            => Host.CreateDefaultBuilder()
                .UseWindowsService()
                .ConfigureServices(x => x
                    .AddSingleton<NatSpec>()
                    .AddHostedService<PumpIn>()
                    .AddHostedService<PumpOut>())
                .RunConsoleAsync();
    }

    public class NatSpec
    {
        public int FindInterfaceIndex()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            return nics
                .First(n => n.Name.Contains("Crosshost"))
                .GetIPProperties()
                .GetIPv4Properties()
                .Index;
        }
    }

    public class PumpIn : Pump
    {
        public PumpIn(NatSpec spec) : base(
            $"ip and ifIdx == {spec.FindInterfaceIndex()} and inbound and ip.DstAddr == 192.168.127.1",
            IPAddress.Parse("127.255.0.1"),
            IPAddress.Parse("127.0.0.1"),
            WinDivertDirection.Outbound,
            true)
        {
        }
    }
    
    public class PumpOut : Pump
    {
        public PumpOut(NatSpec spec) : base(
            "loopback and ip.DstAddr == 127.255.0.1",
            IPAddress.Parse("192.168.127.1"),
            IPAddress.Parse("192.168.127.2"),
            WinDivertDirection.Outbound,
            false)
        {
        }
    }
}