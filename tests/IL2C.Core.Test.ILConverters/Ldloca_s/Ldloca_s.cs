using System;
using System.Runtime.CompilerServices;

namespace IL2C.ILConverters
{
    [TestCase("True", "True")]
    [TestCase("False", "False")]
    [TestCase("255", "Byte")]
    [TestCase("32767", "Int16")]
    [TestCase("2147483647", "Int32")]
    [TestCase("9223372036854775807", "Int64")]
    [TestCase("127", "SByte")]
    [TestCase("65535", "UInt16")]
    [TestCase("4294967295", "UInt32")]
    [TestCase("18446744073709551615", "UInt64")]
    [TestCase("2147483647", "IntPtr")]
    [TestCase("4294967295", "UIntPtr")]
    [TestCase("3.141593", "Single")]            // Lost last 2 digits via ToString conversion.
    [TestCase("3.14159265358979", "Double")]    // Lost last 2 digits via ToString conversion.
    [TestCase("A", "Char")]
    [TestCase("ABC", new[] { "String", "UpdateString" })]               // Translation will include UpdateString method
    [TestCase("ABC", new[] { "LocalVariable_255", "UpdateString" })]    // Translation will include UpdateString method
    public sealed class Ldloca_s
    {
        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string True();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string False();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Byte();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Int16();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Int32();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Int64();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string SByte();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string UInt16();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string UInt32();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string UInt64();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string IntPtr();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string UIntPtr();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Single();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Double();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string Char();

        public static void UpdateString(ref string value)
        {
            value = "ABC";
        }

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string String();

        [MethodImpl(MethodImplOptions.ForwardRef)]
        public static extern string LocalVariable_255();
    }
}
