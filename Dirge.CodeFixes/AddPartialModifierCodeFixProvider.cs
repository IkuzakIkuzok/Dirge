
// (c) 2026 Kazuki Kohzuki

using Dirge.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dirge.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPartialModifierCodeFixProvider)), Shared]
internal sealed class AddPartialModifierCodeFixProvider : CodeFixProvider
{
    override public ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.TypeMustBePartialId];

    override public FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (declaration is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add 'partial' modifier",
                createChangedDocument: c => AddPartialModifierAsync(context.Document, declaration, c),
                equivalenceKey: nameof(AddPartialModifierCodeFixProvider)
            ),
            diagnostic);
    }

    private static async Task<Document> AddPartialModifierAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.ElasticSpace);

        var newModifiers = typeDecl.Modifiers.Add(partialToken);
        var newTypeDecl = typeDecl.WithModifiers(newModifiers);
        editor.ReplaceNode(typeDecl, newTypeDecl);
        return editor.GetChangedDocument();
    } // private static async Task<Document> AddPartialModifierAsync (Document, TypeDeclarationSyntax, CancellationToken)
} // internal sealed class AddPartialModifierCodeFixProvider : CodeFixProvider
