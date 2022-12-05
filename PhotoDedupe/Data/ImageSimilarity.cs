using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDedupe.Data
{
    internal class ImageSimilarity : IDataHolder<string, List<string>>
    {
        public ImageSimilarity()
        {
            Data = new ConcurrentDictionary<string, List<string>>();
        }

        public ConcurrentDictionary<string, List<string>> Data { get; }

        public void Report()
        {
            var uniq_count = 0;
            var dup_count = 0;
            foreach(var item in Data.Values )
            {
                if (item.Count > 1)
                {
                    dup_count += item.Count;
                }
                else
                {
                    uniq_count++;
                }
            }
            var total = uniq_count + dup_count;
            var perc = (Single) dup_count / total;
            Console.WriteLine($"Uniq: {uniq_count}, Dup: {dup_count}, Total: {total}, Dup perc: {perc}");
        }
    }
}
