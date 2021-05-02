using System.Collections.Generic;
using WinDivertSharp;

namespace LocalhostDivert
{
    internal static class Extensions
    {
        public static IEnumerable<byte> ReadBufferBytes(this WinDivertBuffer buff)
        {
            uint len = 0;
            
            for (var i = 0; i < buff.Length; i++)
            {
                var b = buff[i];

                switch (i)
                {
                    case 2:
                        len = (uint)(b << 8);
                        break;
                    case 3:
                        len += (uint)(b & 0xFF);
                        break;
                }

                if (i > 3 && i >= len)
                {
                    yield break;
                }
                else
                {
                    yield return b;
                }
            }
        }
    }
}