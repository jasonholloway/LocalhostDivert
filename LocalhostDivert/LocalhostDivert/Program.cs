using System;
using System.Runtime.InteropServices;
using System.Threading;
using WinDivertSharp;

namespace LocalhostDivert
{
    internal class Program
    {
        private CancellationToken _cancel;
        
        public static void Main(string[] args)
        {
            uint errorPos = 0;

            string filter = "udp";

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
            
                Run(handle);
            }
            finally
            {
                WinDivert.WinDivertClose(handle);
            }
        }

        private static void Run(IntPtr handle)
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

                Console.WriteLine("Read packet {0}", packetSize);
                
                if (!WinDivert.WinDivertSendEx(handle, buffer, packetSize, 0, ref address))
                {
                    throw new Exception($"Write error: {Marshal.GetLastWin32Error()}");
                }
            }
        }
    }
}