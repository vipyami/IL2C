﻿using System;

using Mono.Cecil.Cil;

using IL2C.Translators;

namespace IL2C.ILConverters
{
    internal static class LogicalConverterUtilities
    {
        public enum BinaryOperators
        {
            And,
            Or,
            Xor
        }

        public static Func<IExtractContext, string[]> Apply(BinaryOperators binaryOperator, DecodeContext decodeContext)
        {
            var si1 = decodeContext.PopStack();
            var si0 = decodeContext.PopStack();

            char opChar;
            switch (binaryOperator)
            {
                case BinaryOperators.And: opChar = '&'; break;
                case BinaryOperators.Or: opChar = '|'; break;
                case BinaryOperators.Xor: opChar = '^'; break;
                default: throw new Exception();
            }

            // See also: ECMA-335: III.1.5 Operand type table - Integer Operations

            // Int32 = (Int32) op (Int32)
            if (si0.TargetType.IsInt32StackFriendlyType && si1.TargetType.IsInt32StackFriendlyType)
            {
                var resultName = decodeContext.PushStack(decodeContext.PrepareContext.MetadataContext.Int32Type);
                return _ => new[] { string.Format(
                    "{0} = {1} {2} {3}", resultName, si0.SymbolName, opChar, si1.SymbolName) };
            }

            // Int64 = (Int64) op (Int64)
            if (si0.TargetType.IsInt64StackFriendlyType && si1.TargetType.IsInt64StackFriendlyType)
            {
                var resultName = decodeContext.PushStack(decodeContext.PrepareContext.MetadataContext.Int64Type);
                return _ => new[] { string.Format(
                    "{0} = {1} {2} {3}", resultName, si0.SymbolName, opChar, si1.SymbolName) };
            }

            // IntPtr = (Int32|IntPtr) op (Int32|IntPtr)
            if ((si0.TargetType.IsInt32StackFriendlyType || si0.TargetType.IsIntPtrStackFriendlyType) &&
                (si1.TargetType.IsInt32StackFriendlyType || si1.TargetType.IsIntPtrStackFriendlyType))
            {
                var resultName = decodeContext.PushStack(decodeContext.PrepareContext.MetadataContext.IntPtrType);
                return _ => new[] { string.Format(
                    "{0} = (intptr_t){1} {2} (intptr_t){3}", resultName, si0.SymbolName, opChar, si1.SymbolName) };
            }

            throw new InvalidProgramSequenceException(
                "Unknown logical operation: Location={0}, Op={1}, Type0={2}, Type1={3}",
                decodeContext.CurrentCode.RawLocation,
                binaryOperator,
                si0.TargetType.FriendlyName,
                si1.TargetType.FriendlyName);
        }
    }

    internal sealed class AndConverter : InlineNoneConverter
    {
        public override OpCode OpCode => OpCodes.And;

        public override Func<IExtractContext, string[]> Apply(
            DecodeContext decodeContext)
        {
            return LogicalConverterUtilities.Apply(
                LogicalConverterUtilities.BinaryOperators.And, decodeContext);
        }
    }
}
