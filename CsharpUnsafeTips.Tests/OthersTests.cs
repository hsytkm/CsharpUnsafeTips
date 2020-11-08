using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace CsharpUnsafeTips.Tests
{
    public class OthersTests : IDisposable
    {
        private readonly UnmanagedMemoryContainer _container;

        public OthersTests()
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
        public void Stackalloc()
        {
            var size = 16;
            unsafe
            {
                byte* pointer = stackalloc byte[size];
                for (var i = 0; i < size; ++i)
                {
                    *(pointer + i) = (byte)i;
                }

                Assert.Equal(0x08, *(pointer + 8));
            }
        }

        [Fact]
        public void SpanSlice()
        {
            unsafe
            {
                //var span = new Span<byte>(_container.IntPtr.ToPointer(), _container.Size);
                var span = new ReadOnlySpan<byte>(_container.IntPtr.ToPointer(), _container.Size);
                int start = 128;

                int sum = 0;
                foreach (var b in span.Slice(start, 2))
                {
                    sum += b;
                }

                Assert.Equal(start + (start + 1), sum);
            }

        }

    }
}
