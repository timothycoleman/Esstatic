using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Esstatic {
	public record SeriesSpec(
		string JsonPathExpression,
		string Name,
		Func<IEnumerable<JToken>, JToken> Aggregator,
		Func<JToken, int, int, string> Formatter) {

		public SeriesSpec(
			string jsonPathExpression,
			string name) :
			this(jsonPathExpression, name, DefaultAggregator, DefaultFormatter) { }

		public SeriesSpec(
			string jsonPathExpression,
			string name,
			Func<JToken, int, int, string> formatter) :
			this(jsonPathExpression, name, DefaultAggregator, formatter) {}

		public SeriesSpec(
			string jsonPathExpression,
			string name,
			Func<IEnumerable<JToken>, JToken> aggregator) :
			this(jsonPathExpression, name, aggregator, DefaultFormatter) { }

		public string Render(JObject stats, int column, int row) {
			var line = stats
				.StatsRoot()
				.SelectTokens(JsonPathExpression)
				.Pipe(Aggregator)
				.Pipe(x => x is null ? "" : Formatter(x, column, row));

			return line;
		}

		static JToken DefaultAggregator(IEnumerable<JToken> tokens) => tokens.FirstOrDefault();
		static string DefaultFormatter(JToken token, int column, int row) => $"{token}";
	}
}
