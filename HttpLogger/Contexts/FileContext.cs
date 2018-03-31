using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using HttpLogger.Models;
using NLog;
using System.Linq;
using System.Text;

namespace HttpLogger.Contexts
{
    /// <summary>
    /// Defines the <see cref="FileContext"/> singleton class which provides facilities for querying and intereacting with the file storage of our http trace logs.
    /// </summary>
	public class FileContext : IFileContext
	{
        /// <summary>
        /// Creates a new instance of a <see cref="FileContext"/>. 
        /// </summary>
		public FileContext()
		{
			this.NLogger = NLog.LogManager.GetCurrentClassLogger();
		    this.HttpTraces = new OrderedDictionary();
		}
        
	    /// <summary>
        /// Gets or sets the HttpTrace Entities used to interact with our file storage.
        /// </summary>
		public IOrderedDictionary HttpTraces { get; set; }
		
        /// <summary>
        /// Gets the current classes instance of the NLog <see cref="ILogger"/>
        /// </summary>
		private ILogger NLogger { get; }

        /// <summary>
        /// Saves the changes made to the entities within FileContext.
        /// Saves HttpTraces to {baseDir}/logs/http.txt, while debugging baseDir is located within your bin/Debug.
        /// </summary>
		public void SaveChanges()
		{
			var traceBuilder = new StringBuilder();

			traceBuilder.AppendLine(string.Empty);

			foreach (DictionaryEntry trace in this.HttpTraces)
			{
			    if (!(trace.Key is HttpTrace t))
			        continue;
			    traceBuilder.AppendLine(
			        $"{t.ClientIPAddress} - - [{t.RequestDate:%d/%MMM/%yyyy:%H:%mm:%ss %z}] {t.HttpCommand} {t.StatusCode ?? "-"} {t.ContentSize}");
            } 

			NLogger.Trace(traceBuilder);
		}
	}
}
