using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniNAT
{
    public static class IpOps 
    {
        public static uint CalcChecksum(IEnumerable<byte> bytes)
        {
            uint i = 0;
            uint len = 0;
            uint acc = 0;
            
            foreach (var w in ReadWords(bytes))
            {
                if (i == 0)
                {
                    len = ((w >> 8) & 0xF) * 2;
                }

                if (i != 5)
                {
                    acc += w;
                }

                if (++i >= len) break;
            }

            acc = FoldInCarry(acc);
            acc = FoldInCarry(acc);
            return OnesComplement(acc);

            uint FoldInCarry(uint ac)
                => (ac & 0xFFFF) + (ac >> 16);

            uint OnesComplement(uint ac)
                => ~ac & 0xFFFF;
        }

        public static byte[] ReadHex(string hex)
            => ReadBytes(ReadNibbles(hex)).ToArray();
        
        public static IEnumerable<uint> ReadWords(IEnumerable<byte> bytes)
        {
            var mode = 0;
            uint msb = 0;

            foreach (var b in bytes)
            {
                switch (mode)
                {
                    case 0:
                        msb = b;
                        mode = 1;
                        break;
                    
                    case 1:
                        var lsb = b;
                        yield return (msb << 8) + lsb;
                        mode = 0;
                        break;
                }
            }
        }

        public static IEnumerable<byte> ReadBytes(IEnumerable<int> nibbles)
        {
            var b = 0;
            int msb = 0;

            foreach (var nibble in nibbles)
            {
                switch (b)
                {
                    case 0:
                        msb = nibble;
                        b = 1;
                        break;
                    
                    case 1:
                        var lsb = nibble;
                        yield return (byte)((msb << 4) + lsb);
                        b = 0;
                        break;
                }
            }
        }

        public static IEnumerable<int> ReadNibbles(string hex)
        {
            var i = -1;
            
            foreach (var c in hex)
            {
                i++;
                
                if (char.IsWhiteSpace(c)) continue;

                if (c >= 48 && c < 58)
                {
                    yield return (c - 48);
                    continue;
                }
                
                if (c >= 65 && c <= 70)
                {
                    yield return (c - 65) + 10;
                    continue;
                }
                
                if (c >= 97 && c <= 102)
                {
                    yield return (c - 97) + 10;
                    continue;
                }

                throw new Exception($"Daft character encountered: {c} at pos {i} of {hex}");
            }
        }
    
    }
}