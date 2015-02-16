﻿namespace StyleCop.Analyzers.ReadabilityRules
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Implements a code fix for <see cref="SA1123DoNotPlaceRegionsWithinElements"/> and <see cref="SA1124DoNotUseRegions"/>.
    /// </summary>
    /// <remarks>
    /// <para>To fix a violation of this rule, remove the region.</para>
    /// </remarks>
    [ExportCodeFixProvider(nameof(RemoveRegionCodeFixProvider), LanguageNames.CSharp)]
    [Shared]
    public class RemoveRegionCodeFixProvider : CodeFixProvider
    {
        private static readonly ImmutableArray<string> FixableDiagnostics =
            ImmutableArray.Create(SA1123DoNotPlaceRegionsWithinElements.DiagnosticId, SA1124DoNotUseRegions.DiagnosticId);

        /// <inheritdoc/>
        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return FixableDiagnostics;
        }

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            // The batch fixer does not do a very good job if regions are stacked in each other
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <inheritdoc/>
        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (!this.GetFixableDiagnosticIds().Contains(diagnostic.Id))
                    continue;
                var syntaxRoot = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);
                var node = syntaxRoot?.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
                if (node != null && node.IsKind(SyntaxKind.RegionDirectiveTrivia))
                {
                    var regionDirective = node as RegionDirectiveTriviaSyntax;

                    var newSyntaxRoot = syntaxRoot.RemoveNodes(regionDirective.GetRelatedDirectives(), SyntaxRemoveOptions.AddElasticMarker);

                    context.RegisterFix(CodeAction.Create("Remove region", context.Document.WithSyntaxRoot(newSyntaxRoot)), diagnostic);
                }
            }
        }
    }
}
