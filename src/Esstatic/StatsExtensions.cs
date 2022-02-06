using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esstatic {
	public static class StatsExtensions {
		public static JToken StatsRoot(this JObject x) => x.ContainsKey("stats")
			? x["stats"]
			: x;

		public static string QueryStats(
			this IEnumerable<JObject> statss,
			params IEnumerable<SeriesSpec>[] specss) {

			Console.Write("Querying...");

			var sb = new StringBuilder();

			// add header
			foreach (var specs in specss) {
				sb.AppendJoin('\t', specs.Select(x => x.Name));
				sb.Append("\t\t");
			}
			sb.AppendLine();

			// add rows
			var row = 2; // left room for header
			foreach (var stats in statss) {
				var column = 1;
				foreach (var specs in specss) {
					sb.AppendJoin('\t', specs.Select(spec => {
						return spec.Render(stats, column++, row);
					}));
					sb.Append("\t\t");
					column++;
				}
				sb.AppendLine();
				row++;
			}

			Console.WriteLine(" Done!");
			return sb.ToString();
		}

		public static void CopyToClipBoard(this string s) {
			Console.Write("Copying {0:N0} chars to clipboard...", s.Length);
			TextCopy.ClipboardService.SetText(s);
			Console.WriteLine(" Done!");
		}
	}

	public static class GeneralExtensions {
		public static U Pipe<T, U>(this T t, Func<T, U> f) => f(t);

		// evenly down sample to get desiredCount results.
		// the entries are not combined/interpolated in any way, just selected or discarded.
		public static T[] Scale<T>(
			this IEnumerable<T> input,
			int desiredCount,
			float skipPercent,
			float takePercent,
			T padding) {

			var inputCount = input.Count();

			// skip/take
			var skipCount = (int)(skipPercent * 0.01 * inputCount);
			var takeCount = (int)(takePercent * 0.01 * inputCount);
			var skipTake = input.Skip(skipCount).Take(takeCount);

			// apply skip effect on count
			var skipTakeCount = inputCount;
			skipTakeCount = Math.Max(0, skipTakeCount - skipCount);

			// apply take effect on count
			skipTakeCount = Math.Min(takeCount, skipTakeCount);

			if (skipTakeCount == 0)
				throw new Exception("No stats");

			// now the input has been whittled down to the range we want to sample from
			// but there still be too many 
			var ys = new T[desiredCount];

			if (desiredCount < skipTakeCount) {
				// down sample
				var enumerator = skipTake.GetEnumerator();
				enumerator.MoveNext();
				var enumeratorIndex = 0;

				for (var yi = 0L; yi < ys.Length; yi++) {
					var xi = (int)(yi * skipTakeCount / ys.Length);
					while (enumeratorIndex < xi) {
						if (!enumerator.MoveNext())
							throw new Exception("unexpectedly ran out of things to enumerate");
						enumeratorIndex++;
					}

					ys[yi] = enumerator.Current;
				}
			}
			else {
				// take what we have, add padding.
				var yi = 0;
				foreach (var x in skipTake) {
					ys[yi++] = x;
				}
				while (yi < ys.Length) {
					ys[yi++] = padding;
				}
			}

			return ys;
		}
	}
}
