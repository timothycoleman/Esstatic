using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Esstatic {
	public class StatsReader {
		//
		// Formatters
		//
		string N0(JToken x, int column, int row) => $"{x:N0}";

		string IdlePercent(JToken x, int column, int row) => $"= 100 - {x:N2}";

		Func<JToken, int, int, string> Counter() {
			var i = 0;
			return (_, _, _) => $"{i++}";
		}

		readonly CellReferenceConverter _referencer = new CellReferenceConverter();
		// Positive because for things like disk accesses which only count upwards
		// we dont want a big downward spike if the process restarts.
		string PositiveDelta(JToken t, int column, int row) {
			// references not very elegant.. can we do truly relative references somehow that don't
			// know where they are positioned absolutely in the sheet
			var a = _referencer.ToRelativeCellReference(column - 1, row);
			var b = _referencer.ToRelativeCellReference(column - 1, row - 1);

			return row == 2
				? $""
				: $"= MAX({a} - {b}, 0)";
		}

		//
		// Aggregators
		//
		JToken Max(IEnumerable<JToken> tokens) => tokens.Max();
		JToken Min(IEnumerable<JToken> tokens) => tokens.Min();

		SeriesSpec Placeholder(string name) => new("", name, x => "");

		public void GetStats
			(IEnumerable<string> files,
			float skipPercent,
			float takePercent,
			string query) {

			// for sorting padding to the end so that deltas work properly
			var bigDate = JToken.Parse("\"9999-01-01T00:00:00.0000000Z\"");

			var commandLineSpecs = new List<List<SeriesSpec>>();
			if (!string.IsNullOrEmpty(query))
				commandLineSpecs.Add(
					new List<SeriesSpec> {
						new ("$.timestamp", "timestamp"),
						new (query, "Query", N0),
					});

			var defaultSpecs = new List<SeriesSpec>[] {
					// timeline check
				new List<SeriesSpec> {
						new ("$.timestamp", "entry", Counter()),
						new ("$.timestamp", "timestamp"),
					},

					// restart detector
				new List<SeriesSpec> {
						new ("$.timestamp", "restarts"),
						new ("$.proc.startTime", "startTime"),
						// pid check, in case we accidentally graph two processes for the same period
						new ("$.proc.id", "pid"),
					},

					// read/write operations totals.. also available: proc.readBytes/writtenBytes
					// maybe can convert into rates with relative notation
					new List<SeriesSpec> {
						new ("$.timestamp", "disk operations"),
						new ("$.proc.diskIo.readOps", "readOpsTotal"),
						new ("", "readOpsDelta", PositiveDelta),
						new ("$.proc.diskIo.writeOps", "writeOpsTotal"),
						new ("", "writeOpsDelta", PositiveDelta),
						new ("$.proc.diskIo.readBytes", "readBytesTotal"),
						new ("", "readBytesDelta", PositiveDelta),
						new ("$.proc.diskIo.writtenBytes", "writtenBytesTotal"),
						new ("", "writtenBytesDelta", PositiveDelta),
					},

					// streamInfo
					new List<SeriesSpec> {
						new ("$.timestamp", "streamInfo"),
						new ("$.es.readIndex.cachedStreamInfo", "hitsTotal"),
						new ("", "hitsDelta", PositiveDelta),
						new ("$.es.readIndex.notCachedStreamInfo", "missesTotal"),
						new ("", "missesDelta", PositiveDelta),
						// es.readIndex.cachedRecord
						// es.readIndex.notCachedRecord
						// es.readIndex.cachedTransInfo
						// es.readIndex.notCachedTransInfo
					},

					// misc
					new List<SeriesSpec> {
						new ("$.timestamp", "Misc"),
						new ("$.proc.thrownExceptionsRate", "thrownExceptionsRate", N0),
						new ("$.proc.contentionsRate", "contentionsRate", N0),
						new ("$.proc.threadsCount", "threadsCount", N0),
						new ("$.proc.tcp.connections", "connections", N0),
						new ("$.es.writer.meanFlushSize", "meanFlushSize", N0),
						new ("$.es.writer.meanFlushDelayMs", "meanFlushDelayMs", N0),
						// sys.drive.<path>.availableBytes
						// sys.drive.<path>.totalBytes
						// sys.drive.<path>.usage
						// sys.drive.<path>.usedBytes
						// es.writer.queuedFlushMessages
					},

					// tcp
					new List<SeriesSpec> {
						new ("$.timestamp", "TCP"),
						new ("$.proc.tcp.receivingSpeed", "receivingSpeed", N0),
						new ("$.proc.tcp.sendingSpeed", "sendingSpeed", N0),
						new ("$.proc.tcp.inSend", "inSend", N0),
						new ("$.proc.tcp.pendingReceived", "pendingReceived", N0),
						new ("$.proc.tcp.pendingSend", "pendingSend", N0),
					},

					// memory usage
					new List<SeriesSpec> {
						new ("$.timestamp", "memory Usaage"),
						new ("$.proc.mem", "mem", N0),
						new ("$.proc.gc.allocationSpeed", "allocationSpeed", N0),
						new ("$.proc.gc.totalBytesInHeaps", "totalBytesInHeaps", N0),
						new ("$.proc.gc.gen2Size", "gen2Size", N0),
						new ("$.proc.gc.largeHeapSize", "lohSize", N0),
						new ("$.sys.freeMem", "freemem", N0),
						new ("$.proc.gc.gen2ItemsCount", "gen2Collections", N0),
					},

					// activity %
					new List<SeriesSpec> {
						new ("$.timestamp", "activity %"),
						new ("$.es.queue.MainQueue.idleTimePercent", "MainQueue", IdlePercent),
						new ("$.es.queue.MonitoringQueue.idleTimePercent", "Monitoring", IdlePercent),
						new ("$.es.queue['Storage Chaser'].idleTimePercent", "Chaser", IdlePercent),
						new ("$.es.queue.StorageWriterQueue.idleTimePercent", "Writer", IdlePercent),
						new ("$.es.queue['Index Committer'].idleTimePercent", "IndexCommitter", IdlePercent),
						new ("$.es.queue['Leader Replication Service'].idleTimePercent", "Replication", IdlePercent), // todo: or 'master'
						new ("$.es.queue.Timer.idleTimePercent", "Timer", IdlePercent),
						new ("$.es.queue.Subscriptions.idleTimePercent", "Subscriptions", IdlePercent),
						new ("$.es.queue.PersistentSubscriptions.idleTimePercent", "PersistentSubscriptions", IdlePercent),
						new ("$.es.queue['Projections Leader'].idleTimePercent", "Projections Leader", IdlePercent),
						new ("$.es.queue..[?(@.groupName == 'Projection Core')].idleTimePercent", "Projection Core Max", Min, IdlePercent),
						new ("$.es.queue..[?(@.groupName == 'Workers')].idleTimePercent", "Workers Max", Min, IdlePercent),
						new ("$.es.queue..[?(@.groupName == 'StorageReaderQueue')].idleTimePercent", "Readers Max", Min, IdlePercent),
						new ("$.sys.cpu", "sys cpu", N0),
						new ("$.proc.cpu", "proc cpu", N0),
						new ("$.proc.gc.timeInGc", "gc", N0),
					},

					// queue length
					new List<SeriesSpec> {
						new ("$.timestamp", "queue length"),
						new ("$.es.queue.MainQueue.length", "MainQueue", N0),
						new ("$.es.queue.MonitoringQueue.length", "Monitoring", N0),
						new ("$.es.queue['Storage Chaser'].length", "Chaser", N0),
						new ("$.es.queue.StorageWriterQueue.length", "Writer", N0),
						new ("$.es.queue['Index Committer'].length", "IndexCommitter", N0),
						new ("$.es.queue['Leader Replication Service'].length", "Replication", N0), // todo: or 'master'
						new ("$.es.queue.Timer.length", "Timer", N0),
						new ("$.es.queue.Subscriptions.length", "Subscriptions", N0),
						new ("$.es.queue.PersistentSubscriptions.length", "PersistentSubscriptions", N0),
						new ("$.es.queue['Projections Leader'].length", "Projections Leader", N0),
						new ("$.es.queue..[?(@.groupName == 'Projection Core')].length", "Projection Core Max", Max, N0),
						new ("$.es.queue..[?(@.groupName == 'Workers')].length", "Workers Max", Max, N0),
						new ("$.es.queue..[?(@.groupName == 'StorageReaderQueue')].length", "Readers Max", Max, N0),
					},

					// queue rate in messages per second during active times
					new List<SeriesSpec> {
						new ("$.timestamp", "Avg Processing Time when active"),
						new ("$.es.queue.MainQueue.avgProcessingTime", "MainQueue"),
						new ("$.es.queue.MonitoringQueue.avgProcessingTime", "Monitoring"),
						new ("$.es.queue['Storage Chaser'].avgProcessingTime", "Chaser"),
						new ("$.es.queue.StorageWriterQueue.avgProcessingTime", "Writer"),
						new ("$.es.queue['Index Committer'].avgProcessingTime", "IndexCommitter"),
						new ("$.es.queue['Leader Replication Service'].avgProcessingTime", "Replication", N0), // todo: or 'master'
						new ("$.es.queue.Timer.avgProcessingTime", "Timer", N0),
						new ("$.es.queue.Subscriptions.avgProcessingTime", "Subscriptions", N0),
						new ("$.es.queue.PersistentSubscriptions.avgProcessingTime", "PersistentSubscriptions", N0),
						new ("$.es.queue['Projections Leader'].avgProcessingTime", "Projections Leader", N0),
						new ("$.es.queue..[?(@.groupName == 'Projection Core')].avgProcessingTime", "Projection Core Max", Max, N0),
						new ("$.es.queue..[?(@.groupName == 'Workers')].avgProcessingTime", "Workers Max", Max),
						new ("$.es.queue..[?(@.groupName == 'StorageReaderQueue')].avgProcessingTime", "Readers Max", Max),
				}
			};

			files
				.SelectMany(ReadLines)
				.Scale(
					desiredCount: 200,
					skipPercent: skipPercent,
					takePercent: takePercent,
					padding: "{}")
				.Select(JObject.Parse)
				.OrderBy(x => x.StatsRoot()["timestamp"] ?? bigDate)
				.QueryStats(commandLineSpecs.Any() ? commandLineSpecs.ToArray() : defaultSpecs)
				.Pipe(x => {
					if (commandLineSpecs.Any())
						Console.WriteLine(x.Trim());
					return x;
					})
				.CopyToClipBoard();

		}

		static IEnumerable<string> ReadLines(string path) {
			Console.Write("Reading {0}... ", path);
			using var file = File.OpenText(path);
			var serializer = new JsonSerializer();

			var statss = new List<JObject>();
			while (true) {
				var line = file.ReadLine();

				if (line is null) {
					Console.WriteLine("Done!");
					yield break;
				}

				yield return line;
			}
		}
	}
}
