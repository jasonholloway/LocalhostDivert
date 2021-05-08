using System;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using WinDivertSharp;

namespace MiniNAT
{
    using static IpOps;
    
    public abstract class Pump : BackgroundService
    {
        private readonly string _filter;
        private readonly IPAddress _newSource;
        private readonly byte[] _newSourceBytes;
        private readonly IPAddress _newDest;
        private readonly byte[] _newDestBytes;
        private readonly WinDivertDirection _newDirection;
        private readonly bool _newIsLoopback;

        protected Pump(string filter, IPAddress newSource, IPAddress newDest, WinDivertDirection newDirection, bool newIsLoopback)
        {
            _filter = filter;
            
            _newSource = newSource;
            _newSourceBytes = newSource.GetAddressBytes();
            
            _newDest = newDest;
            _newDirection = newDirection;
            _newIsLoopback = newIsLoopback;
            _newDestBytes = newDest.GetAddressBytes();
        }

        protected override Task ExecuteAsync(CancellationToken cancel)
            => Task.Run(() =>
            {
                uint errorPos = 0;
                if (!WinDivert.WinDivertHelperCheckFilter(_filter, WinDivertLayer.Network, out string errorMsg, ref errorPos))
                {
                    throw new Exception($"{errorMsg} (at pos {errorPos} of '{_filter}')");
                }
                
                var handle = WinDivert.WinDivertOpen(_filter, WinDivertLayer.Network, 0, WinDivertOpenFlags.None);
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

                return;
                
                
                void _Run()
                {
                    using var buffer = new WinDivertBuffer();
                    var address = new WinDivertAddress();

                    while (!cancel.IsCancellationRequested)
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
                        
                        //new source IP address
                        buffer[12] = _newSourceBytes[0];
                        buffer[13] = _newSourceBytes[1];
                        buffer[14] = _newSourceBytes[2];
                        buffer[15] = _newSourceBytes[3];

                        //new dest IP address
                        buffer[16] = _newDestBytes[0];
                        buffer[17] = _newDestBytes[1];
                        buffer[18] = _newDestBytes[2];
                        buffer[19] = _newDestBytes[3];
                        
                        var checksum = CalcChecksum(buffer.ReadBufferBytes());
                        buffer[10] = (byte)((checksum >> 8) & 0xFF);
                        buffer[11] = (byte)(checksum & 0xFF);

                        address.Loopback = _newIsLoopback;
                        address.Direction = _newDirection;

                        var bytesAfter = buffer.ReadBufferBytes().ToArray();
                        Console.WriteLine($"After:\t{BitConverter.ToString(bytesAfter)}");
                        
                        if (!WinDivert.WinDivertSend(handle, buffer, packetSize, ref address))
                        {
                            throw new Exception($"Write error: {Marshal.GetLastWin32Error()}");
                        }
                    }
                }
            });
    }
}