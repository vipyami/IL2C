﻿using System;

using Mono.Cecil;

namespace IL2C.Metadata.Attributes
{
    public sealed class NativeMethodAttributeInformation
    {
        internal NativeMethodAttributeInformation(CustomAttribute attribute)
        {
            this.IncludeFileName = attribute.GetArgument<string>(0);
            this.SymbolName = attribute.GetProperty<string>("SymbolName");
            this.LibraryFileName = attribute.GetProperty<string>("LibraryFileName");
            this.CharSet = attribute.GetProperty(
                "CharSet",
                value => (NativeCharSetInformation)Enum.Parse(typeof(NativeCharSetInformation), value.ToString()));
        }

        public string IncludeFileName { get; }
        public string SymbolName { get; set; }

        public string LibraryFileName { get; set; }
        public NativeCharSetInformation CharSet { get; set; }

        internal static readonly string FullName = "System.Runtime.InteropServices.NativeMethodAttribute";
    }
}
