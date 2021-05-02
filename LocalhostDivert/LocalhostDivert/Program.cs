using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WinDivertSharp;

namespace LocalhostDivert
{
    using static IpOps;
    
    internal class Program
    {
        public static async Task Main()
        {
            Task.Run(Run);
            Task.Run(SnoopFromLocalHost);

            await Task.Delay(1000 * 60 * 10);
        }

        private static void Run()
        {
            uint errorPos = 0;

            string filter = "ip and ifIdx == 13 and inbound and ip.DstAddr == 192.168.127.1";

            if (!WinDivert.WinDivertHelperCheckFilter(filter, WinDivertLayer.Network, out string errorMsg, ref errorPos))
            {
                throw new Exception($"{errorMsg} (at pos {errorPos} of '{filter}')");
            }
            
            var handle = WinDivert.WinDivertOpen(filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            {
                throw new Exception($"Failed to open WinDivert");
            }

            try
            {
                WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueLen, 16384);
                WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueTime, 8000);
                WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueSize, 33554432);
            
                _Run();
            }
            finally
            {
                WinDivert.WinDivertClose(handle);
            }


            void _Run()
            {
                using var buffer = new WinDivertBuffer();
                var address = new WinDivertAddress();

                while (true)
                {
                    uint packetSize = 0;
                     
                    address.Reset();

                    if (!WinDivert.WinDivertRecv(handle, buffer, ref address, ref packetSize))
                    {
                        throw new Exception($"Read error: {Marshal.GetLastWin32Error()}");
                    }

                    Console.WriteLine($"Read packet from if:{address.IfIdx}, {JsonConvert.SerializeObject(address, Formatting.Indented)}");
                    
                    var bytesBefore = buffer.ReadBufferBytes().ToArray();
                    Console.WriteLine($"Before:\t{BitConverter.ToString(bytesBefore)}");
                    
                    //127.2.0.1
                    buffer[12] = 0x7f;
                    buffer[13] = 0x2;
                    buffer[14] = 0x0;
                    buffer[15] = 0x1;

                    //127.0.0.1
                    buffer[16] = 0x7f;
                    buffer[17] = 0x0;
                    buffer[18] = 0x0;
                    buffer[19] = 0x1;
                    
                    var checksum = CalcChecksum(buffer.ReadBufferBytes());
                    buffer[10] = (byte)((checksum >> 8) & 0xFF);
                    buffer[11] = (byte)(checksum & 0xFF);

                    address.Loopback = true;
                    address.Direction = WinDivertDirection.Outbound;

                    var bytesAfter = buffer.ReadBufferBytes().ToArray();
                    Console.WriteLine($"After:\t{BitConverter.ToString(bytesAfter)}");
                    
                    if (!WinDivert.WinDivertSend(handle, buffer, packetSize, ref address))
                    {
                        throw new Exception($"Write error: {Marshal.GetLastWin32Error()}");
                    }
                }
            }
        }

        
        private static void SnoopFromLocalHost()
        {
            uint errorPos = 0;

            string filter = "loopback and ip.DstAddr == 127.2.0.1";

            if (!WinDivert.WinDivertHelperCheckFilter(filter, WinDivertLayer.Network, out string errorMsg, ref errorPos))
            {
                throw new Exception($"{errorMsg} (at pos {errorPos} of '{filter}')");
            }
            
            var handle = WinDivert.WinDivertOpen(filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            {
                throw new Exception($"Failed to open WinDivert");
            }

            try
            {
                WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueLen, 16384);
                WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueTime, 8000);
                WinDivert.WinDivertSetParam(handle, WinDivertParam.QueueSize, 33554432);
            
                _Run();
            }
            finally
            {
                WinDivert.WinDivertClose(handle);
            }

            void _Run()
            {
                using var buffer = new WinDivertBuffer();
                var address = new WinDivertAddress();

                while (true)
                {
                    uint packetSize = 0;
                     
                    address.Reset();

                    if (!WinDivert.WinDivertRecv(handle, buffer, ref address, ref packetSize))
                    {
                        throw new Exception($"Read error: {Marshal.GetLastWin32Error()}");
                    }

                    Console.WriteLine($"->127.9.9.9  Read packet from if:{address.IfIdx}, {JsonConvert.SerializeObject(address, Formatting.Indented)}");
                    
                    var bytesBefore = buffer.ReadBufferBytes().ToArray();
                    Console.WriteLine($"Before:\t{BitConverter.ToString(bytesBefore)}");
                    
                    //192.168.127.1
                    buffer[12] = 0xC0;
                    buffer[13] = 0xA8;
                    buffer[14] = 0x7F;
                    buffer[15] = 0x01;

                    //192.168.127.2
                    buffer[16] = 0xC0;
                    buffer[17] = 0xA8;
                    buffer[18] = 0x7F;
                    buffer[19] = 0x02;
                    
                    var checksum = CalcChecksum(buffer.ReadBufferBytes());
                    buffer[10] = (byte)((checksum >> 8) & 0xFF);
                    buffer[11] = (byte)(checksum & 0xFF);

                    address.IfIdx = 13;
                    address.Loopback = false;
                    address.Direction = WinDivertDirection.Outbound;

                    var bytesAfter = buffer.ReadBufferBytes().ToArray();
                    Console.WriteLine($"After:\t{BitConverter.ToString(bytesAfter)}");
                    
                    if (!WinDivert.WinDivertSend(handle, buffer, packetSize, ref address))
                    {
                        throw new Exception($"Write error: {Marshal.GetLastWin32Error()}");
                    }
                }
            }
        }
        
    }
}