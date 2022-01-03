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
			this IEnumerable<T> xsEnumerable,
			int desiredCount,
			int skipPercent,
			int takePercent,
			T padding) {

			var xs = xsEnumerable.ToArray();

			// skip/take
			var skipCount = (int)(skipPercent * 0.01 * xs.Length);
			var takeCount = (int)(takePercent * 0.01 * xs.Length);
			xs = xs.Skip(skipCount).Take(takeCount).ToArray();
			
			if (xs.Length == 0)
				throw new Exception("No stats");

			var ys = new T[desiredCount];

			if (desiredCount < xs.Length) {
				// down sample
				for (var yi = 0L; yi < ys.Length; yi++) {
					var xi = (int)(yi * xs.Length / ys.Length);
					if (0 <= xi && xi < xs.Length)
						ys[yi] = xs[xi];
				}
			}
			else {
				// take what we have, add padding.
				for (var i = 0; i < ys.Length; i++) {
					ys[i] = i < xs.Length
						? xs[i]
						: padding;
				}
			}

			return ys;
		}
	}
}
