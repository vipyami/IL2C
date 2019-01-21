﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using IL2C.Metadata;
using IL2C.Translators;
using IL2C.Writers;

namespace IL2C
{
    public enum DebugInformationOptions
    {
        None,
        CommentOnly,
        Full
    }

    public static class AssemblyWriter
    {
        private static void InternalWriteAssemblyReferences(
            CodeTextWriter tw,
            TranslateContext translateContext,
            IExtractContext extractContext,
            MemberScopes scope)
        {
            foreach (var assembly in extractContext.EnumerateRegisteredTypes().
                Where(entry => entry.Key == scope).
                SelectMany(entry => entry.Value.Select(type => type.DeclaringModule.DeclaringAssembly)).
                Where(assembly => !assembly.Equals(translateContext.Assembly)).
                Distinct().
                OrderBy(assembly => assembly.Name))
            {
                tw.WriteLine("#include <{0}.h>", assembly.Name);
            }
            tw.SplitLine();
        }

        private static void InternalWriteAssemblyReferences(
            CodeTextWriter tw,
            TranslateContext translateContext,
            IExtractContext extractContext,
            ITypeInformation declaringType)
        {
            foreach (var assembly in extractContext.EnumerateRegisteredTypesByDeclaringType(declaringType).
                Select(type => type.DeclaringModule.DeclaringAssembly).
                Where(assembly => !assembly.Equals(translateContext.Assembly)).
                Distinct().
                OrderBy(assembly => assembly.Name))
            {
                tw.WriteLine("#include <{0}.h>", assembly.Name);
            }
            tw.SplitLine();
        }

        private static void InternalWriteCommonHeader(
            CodeTextStorage storage,
            TranslateContext translateContext,
            PreparedInformations prepared,
            string assemblyName,
            MemberScopes scope)
        {
            IExtractContext extractContext = translateContext;

            var an = (scope == MemberScopes.Public) ? assemblyName : (assemblyName + "_internal");

            using (var twHeader = storage.CreateHeaderWriter(an))
            {
                var assemblyMangledName = Utilities.GetMangledName(an);

                twHeader.WriteLine("#ifndef __{0}_H__", assemblyMangledName);
                twHeader.WriteLine("#define __{0}_H__", assemblyMangledName);
                twHeader.SplitLine();
                twHeader.WriteLine("#pragma once");
                twHeader.SplitLine();
                twHeader.WriteLine("// This is {0} native code translated by IL2C, do not edit.", assemblyName);
                twHeader.SplitLine();

                // Write assembly references.
                InternalWriteAssemblyReferences(
                    twHeader,
                    translateContext,
                    extractContext,
                    scope);

                foreach (var fileName in extractContext.EnumerateRequiredImportIncludeFileNames())
                {
                    twHeader.WriteLine("#include <{0}>", fileName);
                }
                twHeader.SplitLine();

                var expr = (scope == MemberScopes.Public) ?
                    prepared.Types.Where(type => type.IsCLanguagePublicScope) :
                    prepared.Types.Where(type => type.IsCLanguageLinkageScope);

                foreach (var type in expr)
                {
                    twHeader.WriteLine(
                        "#include \"{0}/{1}/{2}.h\"",
                        assemblyName,
                        Utilities.GetCLanguageScopedPath(type.ScopeName),
                        type.Name);
                }

                twHeader.SplitLine();

                if (scope != MemberScopes.Public)
                {
                    var constStrings = extractContext.
                        ExtractConstStrings().
                        ToArray();

                    if (constStrings.Length >= 1)
                    {
                        twHeader.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                        twHeader.WriteLine("// [9-1-1] Const strings:");
                        twHeader.SplitLine();

                        foreach (var (symbolName, _) in extractContext.ExtractConstStrings())
                        {
                            twHeader.WriteLine(
                                "System_String* const {0};",
                                symbolName);
                        }

                        twHeader.SplitLine();
                    }

                    var declaredValues = extractContext.
                        ExtractDeclaredValues().
                        ToArray();

                    if (declaredValues.Length >= 1)
                    {
                        twHeader.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                        twHeader.WriteLine("// [12-1-1] Declared values:");
                        twHeader.SplitLine();

                        foreach (var information in extractContext.ExtractDeclaredValues())
                        {
                            foreach (var declaredFields in information.DeclaredFields)
                            {
                                twHeader.WriteLine(
                                    "// {0}",
                                    declaredFields.FriendlyName);
                            }

                            var targetType = (information.HintTypes.Length == 1) ?
                                information.HintTypes[0] :
                                extractContext.MetadataContext.ByteType.MakeArray();
                            Debug.Assert(targetType.IsArray);

                            var elementType = targetType.ElementType.ResolveToRuntimeType();
                            var values = Utilities.ResourceDataToSpecificArray(information.ResourceData, elementType);

                            var lhs = targetType.GetCLanguageTypeName(information.SymbolName, true);
                            twHeader.WriteLine(
                                "extern const {0};",
                                lhs);
                        }

                        twHeader.SplitLine();
                    }
                }

                twHeader.WriteLine("#endif");
                twHeader.Flush();
            }
        }

        private static void InternalWriteHeader(
            CodeTextStorage storage,
            TranslateContext translateContext,
            PreparedInformations prepared,
            MemberScopes scope)
        {
            IExtractContext extractContext = translateContext;
            var assemblyName = translateContext.Assembly.Name;

            var fieldPredict = (scope == MemberScopes.Public) ?
                new Func<IFieldInformation, bool>(field => field.IsCLanguagePublicScope) :
                new Func<IFieldInformation, bool>(field => field.IsCLanguageLinkageScope);
            var methodPredict = (scope == MemberScopes.Public) ?
                new Func<IMethodInformation, bool>(method => method.IsCLanguagePublicScope && prepared.Functions.ContainsKey(method)) :
                new Func<IMethodInformation, bool>(method => method.IsCLanguageLinkageScope && prepared.Functions.ContainsKey(method));

            foreach (var g in prepared.Types.GroupBy(type => type.ScopeName))
            {
                using (var _ = storage.EnterScope(g.Key))
                {
                    var typeExpr = (scope == MemberScopes.Public) ?
                        g.Where(type => type.IsCLanguagePublicScope) :
                        g.Where(type => type.IsCLanguageLinkageScope);
                    foreach (var type in typeExpr)
                    {
                        using (var twHeader = storage.CreateHeaderWriter(type.Name))
                        {
                            var assemblyMangledName = Utilities.GetMangledName(assemblyName);
                            var scopeName = Utilities.GetMangledName(type.ScopeName);

                            twHeader.WriteLine("#ifndef __{0}_{1}_{2}_H__", assemblyMangledName, scopeName, type.MangledName);
                            twHeader.WriteLine("#define __{0}_{1}_{2}_H__", assemblyMangledName, scopeName, type.MangledName);
                            twHeader.SplitLine();
                            twHeader.WriteLine("#pragma once");
                            twHeader.SplitLine();
                            twHeader.WriteLine("// This is {0} native code translated by IL2C, do not edit.", assemblyName);
                            twHeader.SplitLine();

                            twHeader.WriteLine("#include <{0}.h>", assemblyName);
                            if (scope != MemberScopes.Public)
                            {
                                twHeader.WriteLine("#include <{0}_internal.h>", assemblyName);
                            }
                            twHeader.SplitLine();

                            twHeader.WriteLine("#ifdef __cplusplus");
                            twHeader.WriteLine("extern \"C\" {");
                            twHeader.WriteLine("#endif");
                            twHeader.SplitLine();

                            // All types exclude privates
                            PrototypeWriter.ConvertToPrototype(
                                twHeader,
                                type,
                                fieldPredict,
                                methodPredict);

                            twHeader.WriteLine("#ifdef __cplusplus");
                            twHeader.WriteLine("}");
                            twHeader.WriteLine("#endif");
                            twHeader.SplitLine();

                            twHeader.WriteLine("#endif");
                            twHeader.SplitLine();
                            twHeader.Flush();
                        }
                    }
                }
            }
        }

        public static void WriteHeader(
            CodeTextStorage storage,
            TranslateContext translateContext,
            PreparedInformations prepared)
        {
            var assemblyName = translateContext.Assembly.Name;

            // Write assembly level common public header.
            InternalWriteCommonHeader(
                storage,
                translateContext,
                prepared,
                assemblyName,
                MemberScopes.Public);

            using (var _ = storage.EnterScope(assemblyName, false))
            {
                // Write public headers.
                InternalWriteHeader(
                    storage,
                    translateContext,
                    prepared,
                    MemberScopes.Public);
            }
        }

        ///////////////////////////////////////////////////////

        private static string InternalWriteCommonSource(
            CodeTextStorage storage,
            TranslateContext translateContext,
            PreparedInformations prepared,
            string assemblyName)
        {
            IExtractContext extractContext = translateContext;

            using (var twSource = storage.CreateSourceCodeWriter(assemblyName + "_internal"))
            {
                var assemblyMangledName = Utilities.GetMangledName(assemblyName);

                twSource.WriteLine("// This is {0} native code translated by IL2C, do not edit.", assemblyName);
                twSource.SplitLine();

                twSource.WriteLine("#include <{0}.h>", assemblyName);
                twSource.WriteLine("#include <{0}_internal.h>", assemblyName);
                twSource.SplitLine();

                var constStrings = extractContext.
                    ExtractConstStrings().
                    ToArray();
                if (constStrings.Length >= 1)
                {
                    twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                    twSource.WriteLine("// [9-1-2] Const strings:");
                    twSource.SplitLine();

                    foreach (var (symbolName, value) in extractContext.ExtractConstStrings())
                    {
                        twSource.WriteLine(
                            "IL2C_CONST_STRING({0}, {1});",
                            symbolName,
                            Utilities.GetCLanguageExpression(value));
                    }

                    twSource.SplitLine();
                }

                var declaredValues = extractContext.
                    ExtractDeclaredValues().
                    ToArray();
                if (declaredValues.Length >= 1)
                {
                    twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                    twSource.WriteLine("// [12-1-2] Declared values:");
                    twSource.SplitLine();

                    foreach (var information in extractContext.ExtractDeclaredValues())
                    {
                        foreach (var declaredFields in information.DeclaredFields)
                        {
                            twSource.WriteLine(
                                "// {0}",
                                declaredFields.FriendlyName);
                        }

                        var targetType = (information.HintTypes.Length == 1) ?
                            information.HintTypes[0] :
                            extractContext.MetadataContext.ByteType.MakeArray();
                        Debug.Assert(targetType.IsArray);

                        var elementType = targetType.ElementType.ResolveToRuntimeType();
                        var values = Utilities.ResourceDataToSpecificArray(information.ResourceData, elementType);

                        var lhs = targetType.GetCLanguageTypeName(information.SymbolName, true);
                        twSource.WriteLine(
                            "const {0} =",
                            lhs);
                        using (var _ = twSource.Shift())
                        {
                            twSource.WriteLine(
                                "{0};",
                                Utilities.GetCLanguageExpression(values));
                        }
                    }

                    twSource.SplitLine();
                }

                twSource.Flush();

                return ((CodeTextStorage.InternalCodeTextWriter)twSource).Path;
            }
        }

        private static string[] InternalWriteSourceCode(
            CodeTextStorage storage,
            TranslateContext translateContext,
            PreparedInformations prepared,
            DebugInformationOptions debugInformationOption)
        {
            IExtractContextHost extractContext = translateContext;
            var assemblyName = extractContext.Assembly.Name;

            var typesByDeclaring = prepared.Types.
                GroupBy(type => type.DeclaringType ?? type).
                ToDictionary(g => g.Key, g => g.OrderBy(type => type.UniqueName).ToArray());

            var sourceFiles = new List<string>();

            foreach (var targetType in prepared.Types.
                Where(type => type.DeclaringType == null))
            {
                using (var _ = storage.EnterScope(targetType.ScopeName))
                {
                    using (var twSource = storage.CreateSourceCodeWriter(targetType.Name))
                    {
                        twSource.WriteLine("// This is {0} native code translated by IL2C, do not edit.", assemblyName);
                        twSource.SplitLine();
                        twSource.WriteLine("#include <{0}.h>", assemblyName);
                        twSource.WriteLine("#include <{0}_internal.h>", assemblyName);
                        twSource.SplitLine();

                        // Write assembly references at the file scope.
                        InternalWriteAssemblyReferences(
                            twSource,
                            translateContext,
                            extractContext,
                            targetType);

                        twSource.WriteLine("#ifdef __cplusplus");
                        twSource.WriteLine("extern \"C\" {");
                        twSource.WriteLine("#endif");
                        twSource.SplitLine();

                        foreach (var type in typesByDeclaring[targetType])
                        {
                            twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                            twSource.WriteLine("// [9-2] File scope prototypes:");
                            twSource.SplitLine();

                            // Embeds all types exclude publics and internals (for file scope prototypes)
                            if (type.IsCLanguageFileScope)
                            {
                                PrototypeWriter.ConvertToPrototype(
                                    twSource,
                                    type,
                                    field => field.IsCLanguageFileScope,
                                    method => method.IsCLanguageFileScope && prepared.Functions.ContainsKey(method));
                            }

                            twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                            twSource.WriteLine("// [9-3] Static field instances:");
                            twSource.SplitLine();

                            if (!type.IsEnum)
                            {
                                // All static fields
                                foreach (var field in type.Fields.
                                    Where(field => field.IsStatic))
                                {
                                    if (field.NativeValue == null)
                                    {
                                        twSource.WriteLine(
                                            "{0};",
                                            field.GetCLanguageStaticPrototype(true));
                                    }
                                }
                                twSource.SplitLine();
                            }

                            twSource.WriteLine("//////////////////////////////////////////////////////////////////////////////////");
                            twSource.WriteLine("// [9-4] Type: {0}", type.FriendlyName);
                            twSource.SplitLine();

                            // All methods and constructor exclude type initializer
                            foreach (var method in type.DeclaredMethods.
                                Where(method => prepared.Functions.ContainsKey(method)))
                            {
                                FunctionWriter.InternalConvertFromMethod(
                                    twSource,
                                    extractContext,
                                    prepared,
                                    method,
                                    debugInformationOption);
                            }

                            if (type.IsClass || type.IsValueType)
                            {
                                TypeHelperWriter.InternalConvertTypeHelper(
                                    twSource,
                                    type);
                            }
                            else if (type.IsInterface)
                            {
                                TypeHelperWriter.InternalConvertTypeHelperForInterface(
                                    twSource,
                                    type);
                            }

                            twSource.SplitLine();
                        }

                        twSource.WriteLine("#ifdef __cplusplus");
                        twSource.WriteLine("}");
                        twSource.WriteLine("#endif");
                        twSource.SplitLine();

                        twSource.Flush();

                        sourceFiles.Add(((CodeTextStorage.InternalCodeTextWriter)twSource).Path);
                    }
                }
            }

            return sourceFiles.ToArray();
        }

        private static void InternalWriteBundlerSourceCode(
            CodeTextStorage storage,
            PreparedInformations prepared,
            string assemblyName)
        {
            using (var twSource = storage.CreateSourceCodeWriter(assemblyName + "_bundle"))
            {
                var assemblyMangledName = Utilities.GetMangledName(assemblyName);

                twSource.WriteLine("// This is {0} native code translated by IL2C, do not edit.", assemblyName);
                twSource.SplitLine();
                twSource.WriteLine("// This is the bundler source code for {0},", assemblyName);
                twSource.WriteLine("// you can use it if you wanna compile only one source file.");
                twSource.SplitLine();

                twSource.WriteLine(
                    "#include \"{0}_internal.c\"",
                    assemblyName);

                foreach (var type in prepared.Types)
                {
                    twSource.WriteLine(
                        "#include \"{0}/{1}/{2}.c\"",
                        assemblyName,
                        Utilities.GetCLanguageScopedPath(type.ScopeName),
                        type.Name);
                }

                twSource.SplitLine();
                twSource.Flush();
            }
        }

        public static string[] WriteSourceCode(
            CodeTextStorage storage,
            TranslateContext translateContext,
            PreparedInformations prepared,
            DebugInformationOptions debugInformationOption)
        {
            var sourceFilePaths = new List<string>();

            var assemblyName = translateContext.Assembly.Name;

            // Write assembly level common internal header.
            InternalWriteCommonHeader(
                storage,
                translateContext,
                prepared,
                assemblyName,
                MemberScopes.Linkage);

            // Write assembly level common internal source code.
            sourceFilePaths.Add(
                InternalWriteCommonSource(
                    storage,
                    translateContext,
                    prepared,
                    assemblyName));

            // Write source code bundler.
            InternalWriteBundlerSourceCode(
                storage,
                prepared,
                assemblyName);

            using (var _ = storage.EnterScope(assemblyName, false))
            {
                // Write internal headers.
                InternalWriteHeader(
                    storage,
                    translateContext,
                    prepared,
                    MemberScopes.Linkage);

                // Write source codes.
                sourceFilePaths.AddRange(
                    InternalWriteSourceCode(
                        storage,
                        translateContext,
                        prepared,
                        debugInformationOption));
            }

            return sourceFilePaths.ToArray();
        }
    }
}
