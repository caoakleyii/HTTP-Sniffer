using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpLogger.Contexts;
using HttpLogger.Models;

namespace HttpLogger.Repositories
{
	public class HttpTraceRepository : IHttpTraceRepository
	{
		private FileContext Context   {get; set; }

		public HttpTraceRepository()
		{
			this.Context = FileContext.Instance;
		}
		public void CreateTrace(HttpTrace trace)
		{
			trace.Id = Guid.NewGuid().ToString();

			this.Context.HttpTraces.Add(trace.Id, trace);
		}

		public IDictionary<string, HttpTrace> ReadTraces()
		{
			return this.Context.HttpTraces;
		}

		public HttpTrace ReadTrace(string id)
		{
			return this.Context.HttpTraces[id];
		}

		public void SaveChanges()
		{
			this.Context.SaveChanges();
		}
	}
}
