
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseNameofCodeFixProvider)), Shared]
internal sealed class UseNameofCodeFixProvider : CodeFixProvider
{
    override public ImmutableArray<string> FixableDiagnosticIds => [DiagnosticDescriptors.DoNotDisposeWhenNameShouldBeNameofId];

    override public FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    override public async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var literal = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();
        if (literal is null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use 'nameof' expression",
                createChangedDocument: c => UseNameof(context.Document, literal, c),
                equivalenceKey: nameof(UseNameofCodeFixProvider)
            ),
            diagnostic);
    } // override public async Task RegisterCodeFixesAsync (CodeFixContext)

    async private static Task<Document> UseNameof(Document document, LiteralExpressionSyntax literalExpression, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var fieldName = literalExpression.Token.ValueText;
        var nameofIdentifier = SyntaxFactory.IdentifierName("nameof");
        var nameofExpression = SyntaxFactory.InvocationExpression(nameofIdentifier)
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(fieldName))
                    )
                )
            )
            .WithTriviaFrom(literalExpression);

        editor.ReplaceNode(literalExpression, nameofExpression);
        return editor.GetChangedDocument();
    } // private static Task<Document> UseNameof (Document, LiteralExpressionSyntax, CancellationToken)
} // internal sealed class UseNameofCodeFixProvider : CodeFixProvider
