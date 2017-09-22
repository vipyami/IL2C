﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

namespace IL2C.ILConveters
{
    internal sealed class DecodeContext
    {
        public readonly byte[] ILBytes;
        public readonly Queue<int> PathRemains = new Queue<int>();

        public DecodeContext(byte[] ilBytes)
        {
            this.ILBytes = ilBytes;
            this.PathRemains.Enqueue(0x00);
        }

        public int Index { get; private set; }

        public void ForwardIndex(int offset)
        {
            this.Index += offset;
        }

        public void SetIndex(int newIndex)
        {
            this.Index = newIndex;
        }
    }

    internal abstract class ILConverter
    {
        protected ILConverter()
        {
        }

        public abstract OpCode OpCode { get; }

        public abstract object DecodeOperandAndUpdateNextIndex(DecodeContext context);

        public abstract string Apply(object operand, ApplyContext context);
    }

    internal abstract class InlineNoneConverter : ILConverter
    {
        protected InlineNoneConverter()
        {
        }

        public override object DecodeOperandAndUpdateNextIndex(DecodeContext context)
        {
            return null;
        }
    }

    internal abstract class InlineIConverter : ILConverter
    {
        protected InlineIConverter()
        {
        }

        public override object DecodeOperandAndUpdateNextIndex(DecodeContext context)
        {
            var operand = BitConverter.ToInt32(context.ILBytes, context.Index);
            context.ForwardIndex(sizeof(int));
            return operand;
        }
    }

    internal abstract class InlineI8Converter : ILConverter
    {
        protected InlineI8Converter()
        {
        }

        public override object DecodeOperandAndUpdateNextIndex(DecodeContext context)
        {
            var operand = BitConverter.ToInt64(context.ILBytes, context.Index);
            context.ForwardIndex(sizeof(long));
            return operand;
        }
    }

    internal sealed class NopConverter : InlineNoneConverter
    {
        public NopConverter()
        {
        }

        public override OpCode OpCode => OpCodes.Nop;

        public override string Apply(object operand, ApplyContext context)
        {
            return null;
        }
    }

    internal sealed class Conv_i8Converter : InlineNoneConverter
    {
        public Conv_i8Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Conv_I8;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            if (data0 is int)
            {
                context.EvaluationStack.Push(data0 + "LL");
            }
            else
            {
                context.EvaluationStack.Push("(int64_t)" + data0);
            }
            return null;
        }
    }

    internal sealed class Ldc_i4_1Converter : InlineNoneConverter
    {
        public Ldc_i4_1Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldc_I4_1;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push(1);
            return null;
        }
    }

    internal sealed class Ldc_i4_2Converter : InlineNoneConverter
    {
        public Ldc_i4_2Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldc_I4_2;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push(2);
            return null;
        }
    }

    internal sealed class Ldc_i4Converter : InlineIConverter
    {
        public Ldc_i4Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldc_I4;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push(operand);
            return null;
        }
    }

    internal sealed class Ldc_i8Converter : InlineI8Converter
    {
        public Ldc_i8Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldc_I8;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push(operand + "LL");
            return null;
        }
    }

    internal sealed class Stloc_0Converter : InlineNoneConverter
    {
        public Stloc_0Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Stloc_0;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            return string.Format("local0 = {0};", data0);
        }
    }

    internal sealed class Stloc_1Converter : InlineNoneConverter
    {
        public Stloc_1Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Stloc_1;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            return string.Format("local1 = {0};", data0);
        }
    }

    internal sealed class Stloc_2Converter : InlineNoneConverter
    {
        public Stloc_2Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Stloc_2;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            return string.Format("local2 = {0};", data0);
        }
    }

    internal sealed class Stloc_3Converter : InlineNoneConverter
    {
        public Stloc_3Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Stloc_3;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            return string.Format("local3 = {0};", data0);
        }
    }

    internal sealed class Ldloc_0Converter : InlineNoneConverter
    {
        public Ldloc_0Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldloc_0;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push("local0");
            return null;
        }
    }

    internal sealed class Ldloc_1Converter : InlineNoneConverter
    {
        public Ldloc_1Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldloc_1;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push("local1");
            return null;
        }
    }

    internal sealed class Ldloc_2Converter : InlineNoneConverter
    {
        public Ldloc_2Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldloc_2;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push("local2");
            return null;
        }
    }

    internal sealed class Ldloc_3Converter : InlineNoneConverter
    {
        public Ldloc_3Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldloc_3;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push("local3");
            return null;
        }
    }

    internal sealed class Ldarg_0Converter : InlineNoneConverter
    {
        public Ldarg_0Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldarg_0;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push(context.Parameters[0].Name);
            return null;
        }
    }

    internal sealed class Ldarg_1Converter : InlineNoneConverter
    {
        public Ldarg_1Converter()
        {
        }

        public override OpCode OpCode => OpCodes.Ldarg_1;

        public override string Apply(object operand, ApplyContext context)
        {
            context.EvaluationStack.Push(context.Parameters[1].Name);
            return null;
        }
    }

    internal sealed class AddConverter : InlineNoneConverter
    {
        public AddConverter()
        {
        }

        public override OpCode OpCode => OpCodes.Add;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            var data1 = context.EvaluationStack.Pop();
            var add = string.Format("{0} + {1}", data0, data1);
            context.EvaluationStack.Push(add);
            return null;
        }
    }

    internal sealed class Br_sConverter : ILConverter
    {
        public Br_sConverter()
        {
        }

        public override OpCode OpCode => OpCodes.Br_S;

        public override object DecodeOperandAndUpdateNextIndex(DecodeContext context)
        {
            var operand = (sbyte)context.ILBytes[context.Index];
            context.ForwardIndex(sizeof(sbyte) + operand);
            return operand;
        }

        public override string Apply(object operand, ApplyContext context)
        {
            Debug.Assert(((sbyte)0).Equals(operand));
            return null;
        }
    }

    internal sealed class RetConverter : InlineNoneConverter
    {
        public RetConverter()
        {
        }

        public override OpCode OpCode => OpCodes.Ret;

        public override string Apply(object operand, ApplyContext context)
        {
            var data0 = context.EvaluationStack.Pop();
            return string.Format("return {0};", data0);
        }
    }
}