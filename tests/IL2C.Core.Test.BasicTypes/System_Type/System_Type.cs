
using System;

namespace IL2C.BasicTypes
{
    [TestCase("System.Int32", "FullName", 123)]
    [TestCase("System.String", "FullName", "ABC")]
    // System.Type.ToString() implementation has concatenation for "Type: " header, but the System.RuntimeType do not.
    [TestCase("System.Int32", "ToString", 123)]
    [TestCase("System.String", "ToString", "ABC")]
    [TestCase("System.ValueType", "BaseType", 123)]
    [TestCase("System.Object", "BaseType", "ABC")]
    [TestCase(null, "Object_BaseType")]
    public sealed class System_Type
    {
        public static string ToString(object value)
        {
            return value.GetType().ToString();
        }

        public static string FullName(object value)
        {
            return value.GetType().FullName;
        }

        public static string BaseType(object value)
        {
            return value.GetType().BaseType.FullName;
        }

        public static Type Object_BaseType()
        {
            // TODO: We have to handle GetTypeFromHandle.
            //return typeof(object).BaseType;
            return 123.GetType().BaseType.BaseType.BaseType;
        }
    }
}
