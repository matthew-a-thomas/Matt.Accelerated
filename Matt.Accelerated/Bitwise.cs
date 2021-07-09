using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Matt.Accelerated
{
    public static class Bitwise
    {
        /// <summary>
        /// Performs bitwise XOR from the given <see cref="ReadOnlySpan{T}"/> into the given <see cref="Span{T}"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="Span{T}"/> will be modified.
        /// </para>
        /// </remarks>
        public static void Xor(ReadOnlySpan<byte> from, Span<byte> to)
        {
            if (from.Length != to.Length)
                throw new ArgumentException("The spans have different lengths");
            if (from.Length == 0)
                return;

            if (Avx2.IsSupported)
                XorAvx2(from, to);
            else if (Sse2.IsSupported)
                XorSse2(from, to);
            else if (Environment.Is64BitProcess)
                XorUnrolled64(from, to);
            else
                XorUnrolled32(from, to);
        }

        internal static unsafe void XorAvx2(ReadOnlySpan<byte> from, Span<byte> to)
        {
            fixed (byte* fromPtr = from)
            fixed (byte* toPtr = to)
            {
                var lastOffset = from.Length / 32 * 32;
                for (var offset = 0; offset < lastOffset; offset += 32)
                {
                    var fromCurrentPtr = fromPtr + offset;
                    var toCurrentPtr = toPtr + offset;
                    var fromVector = Avx.LoadVector256(fromCurrentPtr);
                    var toVector = Avx.LoadVector256(toCurrentPtr);
                    var resultVector = Avx2.Xor(fromVector, toVector);
                    Avx.Store(toCurrentPtr, resultVector);
                }
            }
            var remainingBytes = from.Length % 32;
            if (remainingBytes == 0)
                return;
            XorPlainJane(
                from[^remainingBytes..],
                to[^remainingBytes..]
            );
        }

        internal static void XorPlainJane(ReadOnlySpan<byte> from, Span<byte> to)
        {
            for (var i = 0; i < from.Length; ++i)
            {
                to[i] ^= from[i];
            }
        }

        internal static unsafe void XorSse2(ReadOnlySpan<byte> from, Span<byte> to)
        {
            fixed (byte* fromPtr = from)
            fixed (byte* toPtr = to)
            {
                var lastOffset = from.Length / 16 * 16;
                for (var offset = 0; offset < lastOffset; offset += 16)
                {
                    var fromCurrentPtr = fromPtr + offset;
                    var toCurrentPtr = toPtr + offset;
                    var fromVector = Sse2.LoadVector128(fromCurrentPtr);
                    var toVector = Sse2.LoadVector128(toCurrentPtr);
                    var resultVector = Sse2.Xor(fromVector, toVector);
                    Sse2.Store(toCurrentPtr, resultVector);
                }
            }
            var remainingBytes = from.Length % 16;
            if (remainingBytes == 0)
                return;
            XorPlainJane(
                from[^remainingBytes..],
                to[^remainingBytes..]
            );
        }

        internal static void XorUnrolled32(ReadOnlySpan<byte> from, Span<byte> to)
        {
            // XOR ints
            var fromInts = MemoryMarshal.Cast<byte, int>(from);
            var toInts = MemoryMarshal.Cast<byte, int>(to);
            for (var i = 0; i < fromInts.Length; ++i)
            {
                toInts[i] ^= fromInts[i];
            }

            // XOR the dangling bytes
            var remainingBytes = from.Length % 4;
            if (remainingBytes == 0)
                return;
            XorPlainJane(
                from[^remainingBytes..],
                to[^remainingBytes..]
            );
        }

        internal static void XorUnrolled64(ReadOnlySpan<byte> from, Span<byte> to)
        {
            // XOR longs
            var fromLong = MemoryMarshal.Cast<byte, long>(from);
            var toLong = MemoryMarshal.Cast<byte, long>(to);
            for (var i = 0; i < fromLong.Length; ++i)
            {
                toLong[i] ^= fromLong[i];
            }

            // XOR the dangling bytes
            var remainingBytes = from.Length % 8;
            if (remainingBytes == 0)
                return;
            XorPlainJane(
                from[^remainingBytes..],
                to[^remainingBytes..]
            );
        }
    }
}