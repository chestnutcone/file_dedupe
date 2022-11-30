using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace PhotoDedupe
{
    internal class FolderManager
    {
        public FolderManager(string rootDir)
        {
            FileNames = new ConcurrentDictionary<string, List<string>>();
            RootDir = rootDir;
        }
        
        ConcurrentDictionary<string, List<string>> FileNames;
        string RootDir;
        bool CompletedScan = false;

        /// <summary>
        /// Scan all folders recursively from root. Builds a dictinoary FileNames with key of file hash and value list of paths
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            var tasks = Walk(RootDir);
            var task = Task.WhenAll(tasks);
            var total_tasks = tasks.Count;
            // now i can make progress
            while (!task.IsCompleted)
            {
                await Task.Delay(250);
                var completed_tasks = tasks.Where(t => t.IsCompleted).ToList();
                var perc = (Single)completed_tasks.Count / total_tasks;
                perc *= 100;
                Console.Write($"Perc: {Math.Round(perc)}");
            }
            CompletedScan = true;
            Report();
        }

        /// <summary>
        /// Remove duplicate file (file with same hash)
        /// </summary>
        /// <returns></returns>
        public async Task RemoveDeleted()
        {
            if (!CompletedScan)
            {
                await Run();
            }

            // lets keep the first one
            Single idx = 0;
            int max_count = FileNames.Count;
            foreach(var (key, val) in FileNames)
            {
                idx++;

                if (val.Count > 1)
                {
                    for(var i=1; i<val.Count; i++)
                    {
                        File.Delete(val[i]);
                    }
                    FileNames.AddOrUpdate(key, new List<string>(), (k, v) => { return new List<string>() { v[0] }; });
                }
                if (idx % 10 == 0)
                {
                    Console.Write($"Perc: {Math.Round(idx / max_count)}");
                } 
            }

            Report();
        }

        private void Report()
        {
            var uniq_count = 0;
            var dup_count = 0;
            foreach(var item in FileNames.Values )
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
            var files = Directory.GetFiles(workDir);
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
