using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace CsharpUnsafeTips.Tests
{
    public class CopyTest : IDisposable
    {
        private readonly UnmanagedMemoryContainer _container;

        public CopyTest()
        {
            // テスト開始の度にコールされる
            _container = UnmanagedMemoryContainerExtension.GetInstanceBurst256Byte();
        }

        public void Dispose()
        {
            // テスト終了の度にコールされる
            _container.Dispose();
        }

        [Fact]
        public void CopyPointerToPointer_Buffer()
        {
            unsafe
            {
                var size = _container.Size;
                using var destContainer = new UnmanagedMemoryContainer(size);
                void* srcPointer = _container.IntPtr.ToPointer();
                void* destPointer = destContainer.IntPtr.ToPointer();

                // void* srcPointer / void* destPointer
                Buffer.MemoryCopy(srcPointer, destPointer, destinationSizeInBytes: size, sourceBytesToCopy: size);

                Assert.Equal(_container.GetSum(), destContainer.GetSum());
            }
        }

        [Fact]
        public void CopyPointerToPointer_Unsafe()
        {
            var size = _container.Size;

            unsafe
            {
                using var destContainer0 = new UnmanagedMemoryContainer(size);
                void* srcPointer = _container.IntPtr.ToPointer();
                void* destPointer = destContainer0.IntPtr.ToPointer();

                // void* srcPointer / void* destPointer
                Unsafe.CopyBlock(srcPointer, destPointer, (uint)size);

                Assert.Equal(_container.GetSum(), destContainer0.GetSum());
            }

            unsafe
            {
                using var destContainer1 = new UnmanagedMemoryContainer(size);
                void* srcPointer = _container.IntPtr.ToPointer();
                void* destPointer = destContainer1.IntPtr.ToPointer();

                // Unalignedにする必要ない(使いたいだけ)
                Unsafe.CopyBlockUnaligned(srcPointer, destPointer, (uint)size);

                Assert.Equal(_container.GetSum(), destContainer1.GetSum());
            }
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        private static extern void RtlMoveMemory(IntPtr dest, IntPtr src, [MarshalAs(UnmanagedType.U4)] int length);

        [Fact]
        public void CopyPointerToPointer_RtlMoveMemory()
        {
            var size = _container.Size;
            using var destContainer = new UnmanagedMemoryContainer(size);
            var srcIntPtr = _container.IntPtr;
            var destIntPtr = destContainer.IntPtr;

            RtlMoveMemory(destIntPtr, srcIntPtr, size);

            Assert.Equal(_container.GetSum(), destContainer.GetSum());
        }

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)]
        private static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        [Fact]
        public void CopyPointerToPointer_memcpy()
        {
            var size = _container.Size;
            using var destContainer = new UnmanagedMemoryContainer(size);
            var srcIntPtr = _container.IntPtr;
            var destIntPtr = destContainer.IntPtr;

            _ = memcpy(destIntPtr, srcIntPtr, (UIntPtr)size);

            Assert.Equal(_container.GetSum(), destContainer.GetSum());
        }

        [Fact]
        public void CopyPointerToArray()
        {
            var size = _container.Size;
            var srcIntPtr = _container.IntPtr;
            var destArray = new byte[size];

            Marshal.Copy(srcIntPtr, destArray, startIndex: 0, destArray.Length);

            ulong sum = 0;
            foreach (var b in destArray) { sum += b; }
            Assert.Equal(_container.GetSum(), sum);
        }

        [Fact]
        public void CopyArrayToPointer()
        {
            var size = 256;
            var srcArray = Enumerable.Range(0, size).Select(x => (byte)x).ToArray();
            using var destContainer = new UnmanagedMemoryContainer(size);
            var destIntPtr = destContainer.IntPtr;

            Marshal.Copy(srcArray, startIndex: 0, destIntPtr, srcArray.Length);

            ulong sum = 0;
            foreach (var b in srcArray) { sum += b; }
            Assert.Equal(sum, destContainer.GetSum());
        }


    }
}
