using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DapperIntelliSense.Tests.Helpers;
using DapperIntelliSense.Tests.Verification;

namespace DapperIntelliSense.Tests.Tests
{
	class IntelliSenseTextAttribute : EmbeddedFileDataAttribute
	{
		private const string QueryTextPlaceholder = "{QUERY_TEXT}";
		private readonly string _text;

		public IntelliSenseTextAttribute(string text)
			: base("DapperQuery.cs", "User.cs", "Post.cs")
		{
			_text = text;
		}

		public override IEnumerable<object[]> GetData(MethodInfo testMethod)
		{
			foreach (object[] item in base.GetData(testMethod))
			{
				object[] result = new object[item.Length + 1];
				int position = -1;
				for (int i = 0; i < item.Length; i++)
				{
					string docText = (string) item[i];

					if (position < 0)
					{
						position = docText.IndexOf(QueryTextPlaceholder, StringComparison.Ordinal) + _text.Length;
						if (position > 0)
						{
							docText = docText.Replace(QueryTextPlaceholder, _text);
						}
					}

					result[i + 1] = docText;
				}

				result[0] = position;

				yield return result;
			}
		}
	}
}