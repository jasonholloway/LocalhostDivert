using LocalhostDivert;
using NUnit.Framework;

namespace LocalHostDivert.Test
{
    using static IpOps;
    
    [TestFixture]
    public class IpTests
    {
        [Test]
        public void CanReadHex()
            => Assert.That(
                ReadHex("13 aa 409A"), 
                Is.EqualTo(new[] { 0x13, 0xaa, 0x40, 0x9a }));
        
        [Test]
        public void CanCalcChecksum()
        {
            var header = ReadHex("4500 0073 0000 4000 4011 6666 c0a8 0001 c0a8 00c7");
            var checksum = CalcChecksum(header);
            
            Assert.That(checksum, Is.EqualTo(0xb861));
        }
    }
}