using System.Threading.Tasks;
using DapperIntelliSense.Tests.Helpers;
using FluentAssertions;
using Microsoft.CodeAnalysis.Completion;
using Xunit;
using static DapperIntelliSense.Tests.Verification.VerificationHelper;

namespace DapperIntelliSense.Tests.Tests
{
	public class UnitTest1
    {
        [Theory]
		[EmbeddedFileData("NoDapper.cs")]
        public async Task NotADapperCall_ShouldNotShowAnyCustomItems(string source)
        {
	        var document = CreateDocument(source);
	        int position = await GetPositionAsync(document, 9, 23);
	        var service = CompletionService.GetService(document);
	        var actual = await service.GetCompletionsAsync(document, position);

	        actual.Should().BeNull("because it is a completion inside non-Dapper call");
        }

	    [Theory]
	    [IntelliSenseText("")]
	    public async Task EmptyQuery_ShouldShowSelectSuggestion(int position, params string[] sourceFiles)
	    {
		    var document = CreateCSharpDocument(sourceFiles);
		    var service = CompletionService.GetService(document);
		    var actual = await service.GetCompletionsAsync(document, position);

		    actual.Should().NotBeNull();
		    actual.Items.Should().HaveCount(1);
	    }
    }
}
