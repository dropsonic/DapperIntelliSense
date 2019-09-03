using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

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
			if (!(node is ArgumentSyntax argNode) || !(argNode.Expression is LiteralExpressionSyntax literalNode) ||
			    !(argNode.Parent.Parent is InvocationExpressionSyntax invNode))
				return;

			// Check that is is a method call
			if (!(semanticModel.GetSymbolInfo(invNode, cancellationToken).Symbol is IMethodSymbol methodSymbol))
				return;

			// Check that it is a Dapper extension method call
			var sqlMapperSymbol = semanticModel.Compilation.GetTypeByMetadataName("Dapper.SqlMapper");
			if (sqlMapperSymbol == null || !methodSymbol.ContainingType.Equals(sqlMapperSymbol) ||
			    methodSymbol.Name != "Query" || !methodSymbol.IsGenericMethod)
				return;

			// We don't want to show any other IntelliSense items except ours
			context.IsExclusive = true;
			// Get the string literal's value, considering the current position
			string text = literalNode.Token.ValueText;
			text = text.Substring(0, context.Position - literalNode.Token.SpanStart - 1);

			if (String.IsNullOrEmpty(text))
			{
				context.AddItem(CompletionItem.Create("SELECT",
					tags: ImmutableArray.Create(WellKnownTags.Keyword)));
			}
			else
			{
				var parseResult = Parser.Parse(text);

				if (parseResult.Script.Batches.Count == 0 || parseResult.Script.Batches[0].Statements.Count == 0)
					return;
				
				var statement = parseResult.Script.Batches[0].Statements[0];
				if (!(statement is SqlSelectStatement selectStatement))
					return;
				
				if (!(selectStatement.SelectSpecification?.QueryExpression is SqlQuerySpecification query))
					return;

				if (query.SelectClause.SelectExpressions.All(e => String.IsNullOrWhiteSpace(e.Sql)))
				{
					context.AddItem(CompletionItem.Create("TOP", tags: ImmutableArray.Create(WellKnownTags.Keyword)));
					context.AddItem(CompletionItem.Create("*", tags: ImmutableArray.Create(WellKnownTags.Keyword)));

					bool singleTable = methodSymbol.TypeArguments.Length == 1;

					foreach (var typePar in methodSymbol.TypeArguments)
					{
						foreach (var property in typePar.GetMembers()
							.OfType<IPropertySymbol>()
							.Where(p => p.DeclaredAccessibility.HasFlag(Accessibility.Public) && p.GetMethod != null))
						{
							string propText = singleTable ? property.Name : $"{typePar.Name}.{property.Name}";
							context.AddItem(CompletionItem.Create(propText, tags: ImmutableArray.Create(WellKnownTags.Property)));
						}
					}
				}
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
