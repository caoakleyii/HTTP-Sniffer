using System.Collections.Generic;
using System.Collections.Specialized;
using HttpLogger.Models;
using NLog;
using System.Linq;
using System.Text;

namespace HttpLogger.Contexts
{
	public class FileContext
	{
		private FileContext()
		{
			this.NLogger = NLog.LogManager.GetCurrentClassLogger();
			this.HttpTraces = new Dictionary<string, HttpTrace>();
			this.Initalize();
		}

		private void Initalize()
		{

		}

		public static FileContext Instance => Nested.instance;

		public IDictionary<string, HttpTrace> HttpTraces { get; set; }
		
		private Logger NLogger { get; }

		public void SaveChanges()
		{
			var traceBuilder = new StringBuilder();

			traceBuilder.AppendLine(string.Empty);

			this.HttpTraces.ToList().ForEach(trace =>
			{
				traceBuilder.AppendLine(
					$"{trace.Value.ClientIPAddress.ToString()} - - [{trace.Value.RequestDate:%d/%MMM/%yyyy:%H:%mm:%ss %z}] {trace.Value.HttpCommand} {trace.Value.StatusCode} {trace.Value.ContentSize}");
			});

			NLogger.Trace(traceBuilder);
		}

		private class Nested
		{
			// Explicit static constructor to tell C# compiler
			// not to mark type as beforefieldinit
			// http://csharpindepth.com/Articles/General/Beforefieldinit.aspx
			static Nested()
			{

			}

			internal static readonly FileContext instance = new FileContext();
		}
	}
}
