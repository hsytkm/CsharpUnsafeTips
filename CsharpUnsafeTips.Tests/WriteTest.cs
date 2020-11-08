using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace CsharpUnsafeTips.Tests
{
    public class WriteTest : IDisposable
    {
        private readonly UnmanagedMemoryContainer _container;

        public WriteTest()
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
        public void WritePointer_Marshal()
        {
            var intPtr0 = _container.IntPtr;
            var intPtr1 = _container.IntPtr + 8;
            var intPtr2 = _container.IntPtr + 16;
            var intPtr3 = _container.IntPtr + 24;

            Marshal.WriteByte(intPtr0, 0x01);
            Marshal.WriteInt16(intPtr1, 0x0123);
            Marshal.WriteInt32(intPtr2, 0x0123_4567);
            Marshal.WriteInt64(intPtr3, 0x0123_4567_89ab_cdef);

            byte b = Marshal.ReadByte(intPtr0);
            short s = Marshal.ReadInt16(intPtr1);
            int i = Marshal.ReadInt32(intPtr2);
            long l = Marshal.ReadInt64(intPtr3);

            Assert.Equal(0x01, b);
            Assert.Equal(0x0123, s);
            Assert.Equal(0x0123_4567, i);
            Assert.Equal(0x0123_4567_89ab_cdef, l);
        }

        [Fact]
        public void WritePointer_MarshalStructure()
        {
            var intPtr = _container.IntPtr;
            Assert.Equal(0x00, Marshal.ReadByte(intPtr));

            Marshal.StructureToPtr<byte>(0xff, intPtr, fDeleteOld: false);
            Assert.Equal(0xff, Marshal.ReadByte(intPtr));

            var data = new MyStructByte3(0x77, 0x88, 0x99);
            Marshal.StructureToPtr<MyStructByte3>(data, intPtr, fDeleteOld: false);
            var actual = Marshal.ReadInt32(intPtr) & 0x00ff_ffff;
            Assert.Equal(0x0099_8877, actual);
        }

        [Fact]
        public void WritePointer_UnsafeWrite()
        {
            var intPtr0 = _container.IntPtr;
            var intPtr1 = _container.IntPtr + 2;
            var myStruct = new MyStructByte3(0x01, 0x23, 0x45);

            unsafe
            {
                Unsafe.Write<byte>(intPtr0.ToPointer(), 0x01);
            }
            Assert.Equal(0x01, Marshal.ReadByte(intPtr0));

            unsafe
            {
                // Unalignedにする必要ない(使いたいだけ)
                Unsafe.WriteUnaligned<MyStructByte3>(intPtr1.ToPointer(), myStruct);
                Unsafe.WriteUnaligned<byte>((intPtr1 + 3).ToPointer(), 0x00);
            }
            Assert.Equal(0x00_45_23_01, Marshal.ReadInt32(intPtr1));
        }

        [Fact]
        public void WritePointer_Copy()
        {
            var intPtr = _container.IntPtr;
            byte b = 0xff;

            unsafe
            {
                Unsafe.Copy<byte>(intPtr.ToPointer(), ref b);
            }

            Assert.Equal(0xff, Marshal.ReadByte(intPtr));
        }

        [Fact]
        public void FillPointer()
        {
            using var container = UnmanagedMemoryContainerExtension.GetInstanceBurst256Byte();

            var expected0 = Enumerable.Range(0, container.Size).Sum();
            Assert.Equal((ulong)expected0, container.GetSum());

            unsafe
            {
                Unsafe.InitBlock(container.IntPtr.ToPointer(), 0x00, (uint)container.Size);
                Assert.Equal(0UL, container.GetSum());
            }

            unsafe
            {
                // Unalignedにする必要ない(使いたいだけ)
                Unsafe.InitBlockUnaligned(container.IntPtr.ToPointer(), 0x01, (uint)container.Size);
                Assert.Equal((ulong)container.Size, container.GetSum());
            }
        }

    }
}
