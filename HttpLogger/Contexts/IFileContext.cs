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
        IOrderedDictionary HttpTraces { get; set; }

        void SaveChanges();
    }
}
