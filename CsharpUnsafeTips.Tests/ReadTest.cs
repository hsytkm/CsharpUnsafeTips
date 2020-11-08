using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace CsharpUnsafeTips.Tests
{
    public class ReadTest : IDisposable
    {
        private readonly UnmanagedMemoryContainer _container;

        public ReadTest()
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
        public void ReadPointer_Marshal()
        {
            var intPtr0 = _container.IntPtr;
            var intPtr1 = _container.IntPtr + 2;
            var intPtr2 = _container.IntPtr + 4;
            var intPtr3 = _container.IntPtr + 8;

            byte b = Marshal.ReadByte(intPtr0);
            short s = Marshal.ReadInt16(intPtr1);
            int i = Marshal.ReadInt32(intPtr2);
            long l = Marshal.ReadInt64(intPtr3);

            Assert.Equal(0x00, b);
            Assert.Equal(0x0302, s);
            Assert.Equal(0x0706_0504, i);
            Assert.Equal(0x0f0e_0d0c_0b0a_0908, l);
        }

        [Fact]
        public void ReadPointer_MarshalStructure()
        {
            var intPtr = _container.IntPtr;

            var actual0 = Marshal.PtrToStructure<byte>(intPtr + 4);
            Assert.Equal(0x04, actual0);

            var actual1 = Marshal.PtrToStructure<MyStructByte3>(intPtr);
            var expected1 = new MyStructByte3(0x00, 0x01, 0x02);
            Assert.Equal(expected1, actual1);
        }

        [Fact]
        public void ReadPointer_Unsafe()
        {
            var intPtr0 = _container.IntPtr + 4;
            unsafe
            {
                var b = Unsafe.Read<byte>(intPtr0.ToPointer());
                Assert.Equal(0x04, b);
            }


            var intPtr1 = _container.IntPtr + 4;
            var expected = new MyStructByte3(0x04, 0x05, 0x06);

            unsafe
            {
                // Unalignedにする必要ない(使いたいだけ)
                var data = Unsafe.ReadUnaligned<MyStructByte3>(intPtr1.ToPointer());
                Assert.Equal(expected, data);
            }
        }

    }
}
