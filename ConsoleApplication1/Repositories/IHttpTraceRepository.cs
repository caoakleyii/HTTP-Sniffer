using System.Collections.Generic;
using HttpLogger.Models;

namespace HttpLogger.Repositories
{
	public interface IHttpTraceRepository
	{
		void CreateTrace(HttpTrace trace);

		IDictionary<string, HttpTrace> ReadTraces();

		HttpTrace ReadTrace(string id);

		void SaveChanges();
	}
}
