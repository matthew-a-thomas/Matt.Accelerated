namespace Matt.Accelerated.Tests
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.Intrinsics.X86;
    using Xunit;
    using Xunit.Abstractions;

    [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
    public class BitwiseClass
    {
        delegate void TestSpansDelegate(ReadOnlySpan<byte> from, Span<byte> to);

        static void Test(TestSpansDelegate test)
        {
            for (var numBytes = 0; numBytes < 1024; ++numBytes)
            {
                var from = new byte[numBytes];
                for (var i = 0; i < from.Length; ++i)
                {
                    from[i] = 0xa5;
                }
                var to = new byte[numBytes];
                for (var i = 0; i < to.Length; ++i)
                {
                    to[i] = 0x5a;
                }

                test(from, to);

                Assert.Equal<byte>(
                    Enumerable.Repeat((byte)0xff, numBytes),
                    to
                );
            }
        }

        public class XorMethodShould
        {
            [Fact]
            public void ThrowWhenMismatchedLengths()
            {
                var spanA = Array.Empty<byte>();
                var spanB = new byte[1];

                Assert.ThrowsAny<Exception>(() => Bitwise.Xor(spanA, spanB));
            }
        }

        public class XorAvx2MethodShould
        {
            readonly ITestOutputHelper _output;

            public XorAvx2MethodShould(
                ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void Work()
            {
                if (!Avx2.IsSupported)
                {
                    _output.WriteLine("AVX2 isn't supported on this processor");
                    return;
                }

                Test(Bitwise.XorAvx2);
            }
        }

        public class XorPlainJaneMethodShould
        {
            [Fact]
            public void Work()
            {
                Test(Bitwise.XorPlainJane);
            }
        }

        public class XorSse2MethodShould
        {
            readonly ITestOutputHelper _output;

            public XorSse2MethodShould(ITestOutputHelper output)
            {
                _output = output;
            }

            [Fact]
            public void Work()
            {
                if (!Sse2.IsSupported)
                {
                    _output.WriteLine("SSE2 isn't supported on this processor");
                    return;
                }

                Test(Bitwise.XorSse2);
            }
        }

        public class XorUnrolled32MethodShould
        {
            [Fact]
            public void Work()
            {
                Test(Bitwise.XorUnrolled32);
            }
        }

        public class XorUnrolled64MethodShould
        {
            [Fact]
            public void Work()
            {
                Test(Bitwise.XorUnrolled64);
            }
        }
    }
}