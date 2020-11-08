using System;

namespace CsharpUnsafeTips.Tests
{
    class MyPointClass
    {
        public int X;
        public int Y;
        public MyPointClass(int x, int y) => (X, Y) = (x, y);
    }
}
