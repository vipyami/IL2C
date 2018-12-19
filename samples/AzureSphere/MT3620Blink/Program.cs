﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using IL2C;

namespace MT3620Blink
{
    [NativeType("time.h", SymbolName = "struct timespec")]
    internal struct timespec
    {
        public int tv_sec;
        public int tv_nsec;
    }

    [NativeType("applibs/gpio.h")]
    internal enum GPIO_OutputMode_Type
    {
        GPIO_OutputMode_PushPull = 0,
        GPIO_OutputMode_OpenDrain = 1,
        GPIO_OutputMode_OpenSource = 2
    }

    [NativeType("applibs/gpio.h")]
    internal enum GPIO_Value_Type
    {
        GPIO_Value_Low = 0,
        GPIO_Value_High = 1
    }

    public static class Program
    {
        [NativeValue("mt3620_rdb.h")]
        private static readonly int MT3620_RDB_LED1_RED;

        [NativeMethod("applibs/gpio.h")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GPIO_OpenAsOutput(
            int gpioId, GPIO_OutputMode_Type outputMode, GPIO_Value_Type initialValue);

        [NativeMethod("applibs/gpio.h")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern int GPIO_SetValue(int gpioFd, GPIO_Value_Type value);

        [NativeMethod("time.h")]
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void nanosleep(ref timespec time, ref timespec dummy);

        public static int Main()
        {
            var fd = GPIO_OpenAsOutput(
                MT3620_RDB_LED1_RED,
                GPIO_OutputMode_Type.GPIO_OutputMode_PushPull,
                GPIO_Value_Type.GPIO_Value_High);

            var flag = false;
            var sleepTime = new timespec { tv_sec = 1 };
            var dummy = new timespec();
            
            while (true)
            {
                for (var index = 0; index < 10000; index++)
                {
                    GPIO_SetValue(fd, flag ? GPIO_Value_Type.GPIO_Value_High : GPIO_Value_Type.GPIO_Value_Low);
                    flag = !flag;

                    Console.WriteLine("Hello Azure Sphere with C#! " + index);

                    nanosleep(ref sleepTime, ref dummy);
                }
            }
        }
    }
}
