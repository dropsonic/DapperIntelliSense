using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.SqlServer.Management.SqlParser.Parser;

namespace DapperIntelliSense
{
	[ExportCompletionProvider(nameof(DapperCompletionProvider), LanguageNames.CSharp)]
	public class DapperCompletionProvider : CompletionProvider
	{
		public override async Task ProvideCompletionsAsync(CompletionContext context)
		{
			var cancellationToken = context.CancellationToken;
			var syntaxRoot = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var node = syntaxRoot.FindNode(context.CompletionListSpan);
			var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

			// Check that we're inside a string literal which is a method argument
			if (node is ArgumentSyntax argNode && argNode.Expression is LiteralExpressionSyntax literalNode
			    && argNode.Parent.Parent is InvocationExpressionSyntax invNode)
			{
				// Check that is is a Dapper call
				if (semanticModel.GetSymbolInfo(invNode, cancellationToken).Symbol is IMethodSymbol methodSymbol)
				{
					var sqlMapperSymbol = semanticModel.Compilation.GetTypeByMetadataName("Dapper.SqlMapper");
					if (sqlMapperSymbol != null && methodSymbol.ContainingType.Equals(sqlMapperSymbol)
					                            && methodSymbol.Name == "Query")
					{
						// We don't want to show any other IntelliSense items except ours
						context.IsExclusive = true;
						// Get the string literal's value
						string text = literalNode.Token.ValueText;

						if (String.IsNullOrEmpty(text))
						{
							context.AddItem(CompletionItem.Create("SELECT", tags: ImmutableArray.Create(WellKnownTags.Keyword)));
						}
						else
						{
							var parseResult = Parser.Parse(text);
							
						}

					}
				}


				//if (String.IsNullOrEmpty(literal.Token.ValueText))
				//	context.AddItem(CompletionItem.Create("SELECT"));
				//else if (literal.Token.ValueText.StartsWith("SELECT * FROM", StringComparison.OrdinalIgnoreCase))
				//{
					

				//	var dacs = semanticModel.LookupSymbols(context.Position).OfType<INamedTypeSymbol>()
				//		.Where(s => s.AllInterfaces.Any(i => i.Name == "IBqlTable"));

				//	var tags = ImmutableArray.Create(WellKnownTags.Class, WellKnownTags.Public);
				//	foreach (var dacSymbol in dacs)
				//	{
				//		context.AddItem(CompletionItem.Create(dacSymbol.Name, tags: tags));
				//	}
				//}
				//else if (literal.Token.ValueText.StartsWith("SELECT *", StringComparison.OrdinalIgnoreCase))
				//	context.AddItem(CompletionItem.Create("FROM"));
				//else if (literal.Token.ValueText.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
				//	context.AddItem(CompletionItem.Create("*"));
			}
		}
		
		public override bool ShouldTriggerCompletion(SourceText text, int position, CompletionTrigger trigger, OptionSet options)
		{
			switch (trigger.Kind)
			{
				case CompletionTriggerKind.Insertion:
					var insertedCharacterPosition = position - 1;
					return this.IsInsertionTrigger(text, insertedCharacterPosition, options);

				default:
					return false;
			}
		}

		internal virtual bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
		{
			return true;
		}

		//public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
		//{
		//	return Task.FromResult(CompletionDescription.FromText("I am Foo!"));
		//}

		//public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
		//{
		//	return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, "Hello World!")));
		//}
	}
}
