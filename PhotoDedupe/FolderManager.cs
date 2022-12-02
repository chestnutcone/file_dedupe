using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Konsole;

namespace PhotoDedupe
{
    internal class FolderManager
    {
        public FolderManager(string rootDir)
        {
            FileNames = new ConcurrentDictionary<string, List<string>>();
            RootDir = rootDir;
        }
        
        public ConcurrentDictionary<string, List<string>> FileNames;
        protected string RootDir;
        protected bool CompletedScan = false;

        /// <summary>
        /// Scan all folders recursively from root. Builds a dictinoary FileNames with key of file hash and value list of paths
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            var tasks = Walk(RootDir);
            var task = Task.WhenAll(tasks);
            // now i can make progress
            var pb = new ProgressBar(tasks.Count);
            while (!task.IsCompleted)
            {
                await Task.Delay(250);
                var completed_tasks = tasks.Where(t => t.IsCompleted).ToList();
                pb.Refresh(completed_tasks.Count, "Walking");
            }
            CompletedScan = true;
            Report(FileNames);
        }

        /// <summary>
        /// Remove duplicate file (file with same hash)
        /// </summary>
        /// <returns></returns>
        public async Task RemoveDeleted(ConcurrentDictionary<string, List<string>> dict)
        {
            if (!CompletedScan)
            {
                await Run();
            }

            // lets keep the first one
            int idx = 0;
            var pb = new ProgressBar(dict.Count);
            foreach(var (key, val) in dict)
            {
                idx++;

                if (val.Count > 1)
                {
                    for(var i=1; i<val.Count; i++)
                    {
                        File.Delete(val[i]);
                    }
                    dict.AddOrUpdate(key, new List<string>(), (k, v) => { return new List<string>() { v[0] }; });
                }
                if (idx % 10 == 0)
                {
                    pb.Refresh(idx, "Deleting");
                } 
            }

            Report(dict);
        }

        protected void Report(ConcurrentDictionary<string, List<string>> dict)
        {
            var uniq_count = 0;
            var dup_count = 0;
            foreach(var item in dict.Values )
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

        /// <summary>
        /// Walk folder recursively and add tasks to list to be awaited
        /// </summary>
        /// <param name="workDir"></param>
        /// <returns></returns>
        public List<Task> Walk(string workDir)
        {
            var files = FilterFiles(Directory.GetFiles(workDir));
            foreach (var file in files)
            {
                var fileHash = GetHash(file);
                FileNames.AddOrUpdate(
                    fileHash, new List<string>() { file }, (k, v) => { v.Add(file); return v; });
            }

            var folders = Directory.GetDirectories(workDir);
            var tasks = new List<Task>();
            foreach (var folder in folders)
            {
                foreach (var task in Walk(folder))
                {
                    tasks.Add(task);
                }
            }

            return tasks;
        }
        
        protected virtual List<string> FilterFiles(string[] filenames)
        {
            return new List<string>(filenames);
        }

        /// <summary>
        /// Gets file hash
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string GetHash(string filepath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filepath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

    }
}
