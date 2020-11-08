using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace CsharpUnsafeTips.Tests
{
    public class ConvertTests : IDisposable
    {
        private readonly UnmanagedMemoryContainer _container;

        public ConvertTests()
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
        public void ConvertIntPtrToVoidPtr()
        {
            var intPtr = _container.IntPtr;
            unsafe
            {
                void* pointer = intPtr.ToPointer();
                Assert.Equal(intPtr, new IntPtr(pointer));
            }
        }

        [Fact]
        public void ConvertVoidPtrToIntPtr()
        {
            var intPtr = _container.IntPtr;
            unsafe
            {
                void* pointer = intPtr.ToPointer();

                //void* pointer
                IntPtr intPtr1 = new IntPtr(pointer);
                IntPtr intPtr2 = (IntPtr)pointer;

                Assert.Equal(intPtr1, intPtr2);   // どちらの記述でも同じ
            }
        }

        [Fact]
        public void ConvertStackDataToPointer()
        {
            byte b1 = 0x01;
            unsafe
            {
                byte* pointer = &b1;
                *(pointer) += 0x10;

                Assert.Equal(0x11, b1);
            }

            byte b2 = 0x02;
            unsafe
            {
                void* pointer = Unsafe.AsPointer<byte>(ref b2);
                *((byte*)pointer) += 0x20;

                Assert.Equal(0x22, b2);
            }

            byte b3 = 0x03;
            unsafe
            {
                byte* p1 = &b3;
                void* p2 = Unsafe.AsPointer<byte>(ref b3);

                Assert.Equal((IntPtr)p1, (IntPtr)p2);
            }
        }

        [Fact]
        public void ConvertStackDataToRefT()
        {
            byte b1 = 0xff;
            unsafe
            {
                byte* pointer = &b1;

                ref byte x = ref Unsafe.AsRef<byte>(pointer);

                Assert.Equal(0xff, x);
            }
        }

        [Fact]
        public void ConvertArrayToPointer()
        {
            byte[] array = new byte[16];
            for (int i = 0; i < array.Length; ++i) array[i] = (byte)i;

            unsafe
            {
                fixed (byte* ptr = array)
                {
                    byte b10 = *(ptr + 10);

                    Assert.Equal(10, b10);

                    var intPtr = (IntPtr)ptr;
                }
            }
        }

        [Fact]
        public void ConvertPointerToArray()
        {
            // やりようある？
        }

    }
}
