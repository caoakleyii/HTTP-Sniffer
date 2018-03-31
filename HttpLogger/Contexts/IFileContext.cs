using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLogger.Contexts
{
    public interface IFileContext
    {
        /// <summary>
        /// Gets or sets the HttpTrace Entities used to interact with our file storage.
        /// </summary>
        IOrderedDictionary HttpTraces { get; set; }

        /// <summary>
        /// Saves the changes made to the entities within FileContext.
        /// Saves HttpTraces to {baseDir}/logs/http.txt, while debugging baseDir is located within your bin/Debug.
        /// </summary>
        void SaveChanges();
    }
}
