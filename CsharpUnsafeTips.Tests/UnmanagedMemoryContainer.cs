using System;
using System.Runtime.InteropServices;

namespace CsharpUnsafeTips.Tests
{
    class UnmanagedMemoryContainer : IDisposable
    {
        private const int _allocSize = 1 * 1024 * 1024;
        public readonly IntPtr IntPtr;
        public readonly int Size;

        public UnmanagedMemoryContainer(int size)
        {
            IntPtr = Marshal.AllocCoTaskMem(size);
            Size = size;
        }

        public UnmanagedMemoryContainer() : this(_allocSize)
        { }

        public Span<T> ToSpan<T>()
        {
            unsafe { return new Span<T>(IntPtr.ToPointer(), Size); }
        }

        public ReadOnlySpan<T> ToReadOnlySpan<T>()
        {
            unsafe { return new ReadOnlySpan<T>(IntPtr.ToPointer(), Size); }
        }

        public void Dispose()
        {
            Marshal.FreeCoTaskMem(IntPtr);
        }
    }

    static class UnmanagedMemoryContainerExtension
    {
        public static UnmanagedMemoryContainer GetInstanceBurst256Byte()
        {
            var container = new UnmanagedMemoryContainer(256);

            for (var i = 0; i < container.Size; ++i)
            {
                Marshal.WriteByte(container.IntPtr, i, (byte)i);
            }

            return container;
        }

        public static ulong GetSum(this UnmanagedMemoryContainer container)
        {
            ulong sum = 0;

            for (var i = 0; i < container.Size; ++i)
            {
                sum += Marshal.ReadByte(container.IntPtr, i);
            }
            return sum;
        }
    }
}
