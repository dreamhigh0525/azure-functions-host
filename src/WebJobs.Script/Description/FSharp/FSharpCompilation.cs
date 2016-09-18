﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.FSharp.Compiler;
using Microsoft.FSharp.Core;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    internal class FSharpCompilation : ICompilation
    {
        private FSharpErrorInfo[] _errors;
        private FSharpOption<Assembly> _assemblyOption;

        public FSharpCompilation(FSharpErrorInfo[] errors, FSharpOption<Assembly> assemblyOption)
        {
            _errors = errors;
            _assemblyOption = assemblyOption;
        }

        public ImmutableArray<Diagnostic> GetDiagnostics()
        {
            var result = ImmutableArray.CreateBuilder<Diagnostic>();
            foreach (var error in _errors)
            {
                var severity = error.Severity == FSharpErrorSeverity.Error ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
                var descriptor = new DiagnosticDescriptor("FS" + error.ErrorNumber.ToString(), error.Message, error.Message, error.Subcategory, severity, true);
                var location = Location.Create(error.FileName,
                    TextSpan.FromBounds(error.StartColumn, error.EndColumn),
                    new LinePositionSpan(new LinePosition(error.StartLineAlternate, error.StartColumn),
                    new LinePosition(error.EndLineAlternate, error.EndColumn)));

                var diagnostic = Diagnostic.Create(descriptor, location);

                result.Add(diagnostic);
            }
            return result.ToImmutable();
        }

        public FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver)
        {
            EnsureAssemblyOption();

            // Scrape the compiled assembly for entry points
            IList<MethodReference<MethodInfo>> methods =
                            _assemblyOption.Value.GetTypes().SelectMany(t =>
                                t.GetMethods().Select(m =>
                                    new MethodReference<MethodInfo>(m.Name, m.IsPublic, m))).ToList();

            MethodInfo entryPointReference = entryPointResolver.GetFunctionEntryPoint(methods).Value;

            // For F#, this currently creates a malformed signautre with fewer parameter symbols than parameter names.
            // For validation we only need the parameter names. The implementation of DotNetFunctionSignature copes with the 
            // lists having different lengths.
            var parameters = entryPointReference.GetParameters().Select(x => new FunctionParameter(x.Name, x.ParameterType.FullName, x.IsOptional, GetParameterRefKind(x)));
            // For F#, we always set this to true for now.
            bool hasLocalTypeReference = true;

            var signature = new FunctionSignature(entryPointReference.DeclaringType.Name, entryPointReference.Name, parameters.ToImmutableArray(), hasLocalTypeReference);
            
            return signature;
        }

        private static RefKind GetParameterRefKind(ParameterInfo x)
        {
            if (x.IsOut)
            {
                return RefKind.Out;
            }

            return RefKind.None;
        }

        public Assembly EmitAndLoad(CancellationToken cancellationToken)
        {
            EnsureAssemblyOption();

            return _assemblyOption.Value;
        }

        private void EnsureAssemblyOption()
        {
            if (_assemblyOption == null)
            {
                var diagnostics = this.GetDiagnostics();
                var diagnosticsText = new System.Text.StringBuilder();
                foreach (var diagostic in diagnostics)
                {
                    diagnosticsText.Append(diagostic.ToString());
                }

                throw new CompilationErrorException("Script compilation failed. " + diagnosticsText, this.GetDiagnostics());
            }
        }
    }
}
