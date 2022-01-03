using Microsoft.Extensions.FileSystemGlobbing;
using System;

namespace Esstatic {
	class Program {
		/// <param name="dir">Directory to search.</param>
		/// <param name="include">Glob pattern to include.</param>
		/// <param name="exclude">Glob pattern to exclude.</param>
		/// <param name="skip">Percentage of total entries to skip.</param>
		/// <param name="take">Percentage of total entries to take after skip.</param>
		/// <param name="test">Determine which files will be included.</param>
		static void Main(
			string dir = ".",
			string include = "**/*stats*.json",
			string exclude = null,
			int skip = 0,
			int take = 100,
			bool test = false) {

			Console.WriteLine("");
			Console.WriteLine("Arguments:");
			Console.WriteLine("  dir: {0}", dir);
			Console.WriteLine("  include: {0}", include);
			Console.WriteLine("  exclude: {0}", exclude);
			Console.WriteLine("  skip: {0}", skip);
			Console.WriteLine("  take: {0}", take);
			Console.WriteLine("  test: {0}", test);
			Console.WriteLine("");

			var matcher = new Matcher().AddInclude(include);

			if (exclude is not null) {
				matcher = matcher.AddExclude(exclude);
			}

			var files = matcher.GetResultsInFullPath(dir);

			if (test) {
				Console.WriteLine("Matched files: ");
				foreach (var file in files)
					Console.WriteLine(" - " + file);
			} else {
				new StatsReader().GetStats(files, skip, take);
			}

			Console.WriteLine("");
		}
	}
}
