using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoDedupe.Data
{
    internal class FileSizes : IDataHolder<string, long>
    {
        public FileSizes()
        {
            Data = new ConcurrentDictionary<string, long>();
        }
        public ConcurrentDictionary<string, long> Data { get; }

        
        public void Report()
        {
            // report top 20 files with their sizes
            var arr = Data.ToArray();
            Array.Sort(arr, (x, y) => y.Value.CompareTo(x.Value));

            for (var i = 0; i < arr.Length; i++)
            {
                if (i > 20) { break; }

                var item = arr[i];
                Console.WriteLine($"{item.Key} {BytesToString(item.Value)}");
            }
        }

        private String BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}
