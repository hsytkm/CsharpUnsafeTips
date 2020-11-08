using System;
using System.Runtime.InteropServices;

namespace CsharpUnsafeTips.Tests
{
    [StructLayout(LayoutKind.Sequential, Size = 3)]
    struct MyStructByte3
    {
        public byte Byte0;
        public byte Byte1;
        public byte Byte2;

        public MyStructByte3(byte b0, byte b1, byte b2)
        {
            Byte0 = b0;
            Byte1 = b1;
            Byte2 = b2;
        }
    }
}
