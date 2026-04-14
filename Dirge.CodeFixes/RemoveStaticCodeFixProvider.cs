
// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Dirge.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofCodeFixProvider)), Shared]
internal sealed class RemoveStaticCodeFixProvider : CodeFixProvider
{
    override public ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.StaticClassNotSupportedId];

    override public FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    async public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (declaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove 'static' modifier",
                createChangedDocument: c => RemoveStatic(context.Document, declaration, c),
                equivalenceKey: nameof(RemoveStaticCodeFixProvider)
            ),
            diagnostic);
    } // async public override Task RegisterCodeFixesAsync (CodeFixContext)

    async private static Task<Document> RemoveStatic(Document document, TypeDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
    {
        var staticModifier = classDeclaration.Modifiers.FirstOrDefault(m => m.IsKind(SyntaxKind.StaticKeyword));

        if (staticModifier == default)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newModifiers = classDeclaration.Modifiers.Remove(staticModifier);
        var newClassDeclaration = classDeclaration.WithModifiers(newModifiers);

        if (classDeclaration.Modifiers.First().IsKind(SyntaxKind.StaticKeyword))
            newClassDeclaration = newClassDeclaration.WithLeadingTrivia(staticModifier.LeadingTrivia);

        editor.ReplaceNode(classDeclaration, newClassDeclaration);

        return editor.GetChangedDocument();
    } // async private static Task<Document> RemoveStatic (Document, TypeDeclarationSyntax, CancellationToken
}
