using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDedupe.Data
{
    internal interface IDataHolder<TKey, TVal> where TKey : notnull
    {
        ConcurrentDictionary<TKey, TVal> Data { get; }
        void Report();
    }
}
