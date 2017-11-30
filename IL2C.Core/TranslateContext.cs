﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Mono.Cecil;

using IL2C.Translators;

namespace IL2C
{
    public sealed class TranslateContext : IPrepareContext, IExtractContext
    {
        private static readonly Dictionary<string, string> predefinedIncludeHints = new Dictionary<string, string>
        {
            { typeof(bool).FullName, "stdbool.h" },
            { typeof(byte).FullName, "stdint.h" },
            { typeof(sbyte).FullName, "stdint.h" },
            { typeof(short).FullName, "stdint.h" },
            { typeof(ushort).FullName, "stdint.h" },
            { typeof(int).FullName, "stdint.h" },
            { typeof(uint).FullName, "stdint.h" },
            { typeof(long).FullName, "stdint.h" },
            { typeof(ulong).FullName, "stdint.h" },
        };

        private static readonly Dictionary<string, string> predefinedCTypeNames = new Dictionary<string, string>
        {
            { typeof(void).FullName, "void" },
            { typeof(bool).FullName, "bool" },
            { typeof(byte).FullName, "uint8_t" },
            { typeof(sbyte).FullName, "int8_t" },
            { typeof(short).FullName, "int16_t" },
            { typeof(ushort).FullName, "uint16_t" },
            { typeof(int).FullName, "int32_t" },
            { typeof(uint).FullName, "uint32_t" },
            { typeof(long).FullName, "int64_t" },
            { typeof(ulong).FullName, "uint64_t" },
        };

        private readonly HashSet<string> includes = new HashSet<string>();
        private readonly HashSet<string> privateIncludes = new HashSet<string>();
        private readonly Dictionary<string, FieldReference> staticFields =
            new Dictionary<string, FieldReference>();

        public TranslateContext(Assembly assembly)
            : this(assembly.Location)
        {
            includes.Add("il2c.h");
        }

        public TranslateContext(string assemblyPath)
        {
            var resolver = new BasePathAssemblyResolver(Path.GetDirectoryName(assemblyPath));
            var parameter = new ReaderParameters
            {
                AssemblyResolver = resolver
            };

            this.Assembly = AssemblyDefinition.ReadAssembly(assemblyPath, parameter);

            includes.Add("il2c.h");
        }

        public AssemblyDefinition Assembly { get; }

        #region IPrepareContext
        private void RegisterIncludeFile(string includeFileName)
        {
            includes.Add(includeFileName);
        }

        void IPrepareContext.RegisterIncludeFile(string includeFileName)
        {
            this.RegisterIncludeFile(includeFileName);
        }

        private void RegisterPrivateIncludeFile(string includeFileName)
        {
            privateIncludes.Add(includeFileName);
        }

        void IPrepareContext.RegisterPrivateIncludeFile(string includeFileName)
        {
            this.RegisterPrivateIncludeFile(includeFileName);
        }

        private void RegisterType(TypeReference type)
        {
            if (predefinedIncludeHints.TryGetValue(type.FullName, out var includeFile))
            {
                this.RegisterIncludeFile(includeFile);
            }
            else if (type.IsByReference)
            {
                var dereferencedType = type.GetElementType();
                this.RegisterType(dereferencedType);
            }
        }

        void IPrepareContext.RegisterType(TypeReference type)
        {
            this.RegisterType(type);
        }

        private void RegisterStaticField(FieldReference staticField)
        {
            Debug.Assert(staticField.Resolve().IsStatic);

            staticFields.Add(staticField.FullName, staticField);
        }

        void IPrepareContext.RegisterStaticField(FieldReference staticField)
        {
            this.RegisterStaticField(staticField);
        }
        #endregion

        #region IExtractContext
        IEnumerable<string> IExtractContext.EnumerateRequiredIncludeFileNames()
        {
            return includes;
        }

        IEnumerable<string> IExtractContext.EnumerateRequiredPrivateIncludeFileNames()
        {
            return privateIncludes;
        }

        IEnumerable<FieldReference> IExtractContext.ExtractStaticFields()
        {
            return staticFields.Values;
        }

        private string GetCLanguageTypeName(TypeReference type, TypeNameFlags flags = TypeNameFlags.Strict)
        {
            if (predefinedCTypeNames.TryGetValue(type.FullName, out var cTypeName))
            {
                return cTypeName;
            }

            if (type.IsByReference)
            {
                var dereferencedType = type.GetElementType();
                var name = this.GetCLanguageTypeName(dereferencedType);
                return ((flags & TypeNameFlags.Dereferenced) == TypeNameFlags.Dereferenced)
                    ? name : (name + "*");
            }
            else
            {
                var name = type.GetFullMemberName().ManglingSymbolName();
                if ((flags & TypeNameFlags.StructPrefix) == TypeNameFlags.StructPrefix)
                {
                    name = "struct " + name;
                }

                if (type.IsValueType == false)
                {
                    return ((flags & TypeNameFlags.Dereferenced) == TypeNameFlags.Dereferenced)
                        ? name : (name + "*");
                }
                else
                {
                    return name;
                }
            }
        }

        string IExtractContext.GetCLanguageTypeName(TypeReference type, TypeNameFlags flags)
        {
            return this.GetCLanguageTypeName(type, flags);
        }

        private string GetRightExpression(TypeReference lhsType, SymbolInformation rhs)
        {
            if (lhsType.MemberEquals(rhs.TargetType))
            {
                return rhs.SymbolName;
            }

            if (lhsType.IsAssignableFrom(rhs.TargetType))
            {
                Debug.Assert(lhsType.IsValueType == false);
                Debug.Assert(rhs.TargetType.IsValueType == false);

                return String.Format(
                    "({0}){1}",
                    this.GetCLanguageTypeName(lhsType),
                    rhs.SymbolName);
            }

            if (rhs.TargetType.IsNumericPrimitive())
            {
                if (lhsType.IsNumericPrimitive())
                {
                    return String.Format(
                        "({0}){1}",
                        this.GetCLanguageTypeName(lhsType),
                        rhs.SymbolName);
                }
                else if (lhsType.IsBooleanType())
                {
                    return String.Format(
                        "{0} ? true : false",
                        rhs.SymbolName);
                }
            }
            else if (rhs.TargetType.IsBooleanType())
            {
                if (lhsType.IsNumericPrimitive())
                {
                    return String.Format(
                        "{0} ? 1 : 0",
                        rhs.SymbolName);
                }
            }

            return null;
        }

        string IExtractContext.GetRightExpression(TypeReference lhsType, SymbolInformation rhs)
        {
            return this.GetRightExpression(lhsType, rhs);
        }

        private string GetRightExpression(
            TypeReference lhsType, TypeReference rhsType, string rhsExpression)
        {
            if (lhsType.IsAssignableFrom(rhsType))
            {
                return rhsExpression;
            }

            if (rhsType.IsNumericPrimitive())
            {
                if (lhsType.IsNumericPrimitive())
                {
                    return String.Format(
                        "({0})({1})",
                        this.GetCLanguageTypeName(lhsType),
                        rhsExpression);
                }
                else if (lhsType.IsBooleanType())
                {
                    return String.Format(
                        "({0}) ? true : false",
                        rhsExpression);
                }
            }
            else if (lhsType.IsBooleanType())
            {
                if (lhsType.IsNumericPrimitive())
                {
                    return String.Format(
                        "({0}) ? 1 : 0",
                        rhsExpression);
                }
            }

            return null;
        }

        string IExtractContext.GetRightExpression(
            TypeReference lhsType, TypeReference rhsType, string rhsExpression)
        {
            return this.GetRightExpression(lhsType, rhsType, rhsExpression);
        }
        #endregion
    }
}