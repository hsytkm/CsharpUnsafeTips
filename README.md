
## 初めに

本記事は [@gushwell](https://qiita.com/gushwell)さんの [C#リフレクションTIPS 55連発](https://qiita.com/gushwell/items/91436bd1871586f6e663) に触発されて書きました。

 [C#リフレクションTIPS 55連発](https://qiita.com/gushwell/items/91436bd1871586f6e663) は、薄っすらとしか覚えていないリフレクションが網羅的にまとめられており、その便利さのおかげで 余計に リフレクションを覚えられなくなってしまった良記事です:thumbsup_tone1:



私は業務で画像を扱うことが多く、P/Invoke でメモリを受け渡したり、ポインタ越しにメモリを操作するのですが、その度に過去のコードを探ったり、ぐぐったりしています。

ポインタに関わらず大体のことは ぐぐれば出てくるのですが、手間なので、未来の自分のため unsafeポインタ についてまとめました。



## 前提

これから示すコード（のほとんど）は、unsafe コンパイラオプションの有効化 と 下記usingディレクティブ が前提となっています。

動作は .NET Core 3.1 で確認しています。

```C#
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
```



## 型変換

System.IntPtr と void* は相互に変換できます。



### 1. Convert IntPtr -> Pointer

```C#
unsafe {
    // IntPtr intPtr
    void* pointer = intPtr.ToPointer();
}
```



### 2. Convert Pointer -> IntPtr

```C#
unsafe {
    // void* pointer
    IntPtr intPtr0 = new IntPtr(pointer);
    IntPtr intPtr1 = (IntPtr)pointer;	// どちらも同じ
}
```



### 3. Convert StackData -> Pointer

どちらでも結果は同じなので、お好みで使えば良いと思います。

```C#
byte b;
unsafe {
    byte* pointer = &b;
}
```

```C#
byte b = 0x00;
unsafe {
    void* pointer = Unsafe.AsPointer<byte>(ref b);
}
```



### 4. Convert Pointer -> ref T

```C#
unsafe {
    // void* pointer
    ref byte b = ref Unsafe.AsRef<byte>(pointer);
}
```



### 5. Convert Array -> Pointer

マネージドオブジェクトはヒープ領域で管理されているので、GCの再配置を防ぐため fixed を使う必要があります。

```C#
byte[] array = new byte[size];
unsafe {
    fixed (byte* ptr = array) {
    }
}
```



## 読み込み

### 6. Read from IntPtr

ポインタから値を読み込む。

```C#
// unsafe不要
byte b = Marshal.ReadByte(intPtr);
short s = Marshal.ReadInt16(intPtr);
int i = Marshal.ReadInt32(intPtr);
long l = Marshal.ReadInt64(intPtr);

MyStruct st = Marshal.PtrToStructure<MyStruct>(intPtr);
```



### 7. Read from Pointer

ポインタから値を読み込む。

```C#
unsafe {
    // void* pointer
    byte b = Unsafe.Read<byte>(pointer);

    // アライメントを考慮しない版
    MyStruct s = Unsafe.ReadUnaligned<MyStruct>(pointer);
}
```



## 書き込み

### 8. Write to IntPtr

ポインタに値を書き込む。

```C#
// unsafe不要
Marshal.WriteByte(intPtr, 0x01);
Marshal.WriteInt16(intPtr, 0x0123);
Marshal.WriteInt32(intPtr, 0x0123_4567);
Marshal.WriteInt64(intPtr, 0x0123_4567_89ab_cdef);

Marshal.StructureToPtr<MyStruct>(myStruct, intPtr, fDeleteOld: false);
```



### 9. Write to Pointer

ポインタに値を書き込む。

Write() と Copy() が提供されていますが、参照渡しできる Copy() の方がパフォーマンスが良さそうです。

```C#
unsafe {
    // void* pointer
    Unsafe.Write<byte>(pointer0, 0xff);

    // アライメントを考慮しない版
    Unsafe.WriteUnaligned<MyStruct>(pointer1, myStruct);

    byte b = 0x00;
    Unsafe.Copy<byte>(pointer2, ref b);
}
```



### 10. Fill Pointer

ポインタから指定サイズ分だけ、値(byte)を書き込む。

```C#
unsafe {
    // void* pointer
    Unsafe.InitBlock(pointer, 0x00, (uint)size);

    // アライメントを考慮しない版
    Unsafe.InitBlockUnaligned(pointer, 0x00, (uint)size);
}
```



## コピー

### 11. Copy Pointer -> Pointer 

#### Unsafe

```C#
unsafe {
    // void* srcPointer / void* destPointer
    Unsafe.CopyBlock(srcPointer, destPointer, (uint)size);

    // アライメントを考慮しない版
    Unsafe.CopyBlockUnaligned(srcPointer, destPointer, (uint)size);
}
```

#### Buffer

Unsafeクラスには存在しない書き込み先メモリの使用可能なバイト数を設定する引数が存在しますが、使う場面が分からないので、上の Unsafe.CopyBlock() を使っておけば良さそうです。

``` C#
unsafe {
    // void* srcPointer / void* destPointer
    Buffer.MemoryCopy(srcPointer, destPointer, size, size);
}
```



### 12. Copy IntPtr -> IntPtr

動作は速いが、マルチプラットフォームで動作しません。

#### RtlMoveMemory() @kernel32.dll 

```C#
// unsafe不要
[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
private static extern void RtlMoveMemory(IntPtr dest, IntPtr src, [MarshalAs(UnmanagedType.U4)] int length);

RtlMoveMemory(destIntPtr, srcIntPtr, length);
```

#### memcpy @msvcrt.dll

```C#
// unsafe不要
[DllImport("msvcrt.dll", EntryPoint = "memcpy", SetLastError = false)]
private static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

_ = memcpy(destIntPtr, srcIntPtr, (UIntPtr)length);
```



### 13. Copy IntPtr -> Array

```C#
// unsafe不要
Marshal.Copy(srcIntPtr, destArray, startIndex: 0, destArray.Length);
```



### 14. Copy Array -> IntPtr

```C#
// unsafe不要
Marshal.Copy(srcArray, startIndex: 0, destIntPtr, srcArray.Length);
```



## その他

### 15. アンマネージドメモリの確保

AllocCoTaskMem() を使う方が良さげです。

[AllocHGlobalとAllocCoTaskMem どちらを使うべきか？ - Qiita](https://qiita.com/Nuits/items/9dc67cb12e2dcf8d09bd)

#### Marshl.AllocCoTaskMem()

```C#
// unsafe不要
IntPtr intPtr = Marshal.AllocCoTaskMem(allocSize);
// do something
Marshal.FreeCoTaskMem(intPtr);
```

#### Marshal.AllocHGlobal()

```C#
// unsafe不要
IntPtr intPtr = Marshal.AllocHGlobal(allocSize);
// do something
Marshal.FreeHGlobal(intPtr);
```



### 16. スタックメモリのポインタ取得

```C#
unsafe {
    byte* pointer = stackalloc byte[size];
}
```



### 17. Span化

```C#
unsafe {
    // void* pointer
    var span = new Span<byte>(pointer, length);
    var roSpan = new ReadOnlySpan<byte>(pointer, length);
}
```



## 終わりに

30連発くらいにはなるかと思っていましたが、遠く及びませんでした…



