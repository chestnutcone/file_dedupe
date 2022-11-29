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
