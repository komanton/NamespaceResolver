using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NamespaceResolver
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NamespaceResolvingAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = "NR0001";
        private static readonly LocalizableString Title = "Namespace resolver";
        private static readonly LocalizableString MessageFormat = "Namespace must match file location";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault : true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
			context.RegisterCompilationStartAction((compilationContext) =>
            {
                compilationContext.RegisterSyntaxTreeAction((syntaxTreeContext) =>
                {
                    var semModel = compilationContext.Compilation.GetSemanticModel(syntaxTreeContext.Tree);
                    var filePath = syntaxTreeContext.Tree.FilePath;
					
                    if (filePath == null)
                        return;

                    var parentDirectory = System.IO.Path.GetDirectoryName(filePath);

                    var parentDirectoryWithDots = parentDirectory.Replace(Path.DirectorySeparatorChar, '.');

                    var namespaceNodes = syntaxTreeContext.Tree
                        .GetRoot()
                        .DescendantNodes()
                        .OfType<NamespaceDeclarationSyntax>();

                    foreach (var ns in namespaceNodes)
                    {
						var symbolInfo = semModel.GetDeclaredSymbol(ns) as INamespaceSymbol;
                        var name = symbolInfo.ToDisplayString();
						
						if (!parentDirectoryWithDots.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                        {							
                            syntaxTreeContext.ReportDiagnostic(Diagnostic.Create(
                                Rule, ns.Name.GetLocation(), parentDirectoryWithDots));
                        }
                    }
                });
            });
        }
    }
}
