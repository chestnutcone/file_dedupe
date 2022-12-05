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
using System.Diagnostics;
using System.IO.Hashing;
using PhotoDedupe.Data;

namespace PhotoDedupe
{
    internal class FolderManager
    {
        public FolderManager(string rootDir)
        {
            RootDir = rootDir;
            FileDuplicates = new FileDuplicates();
            FileSizes = new FileSizes();
        }
        public IDataHolder<string, List<string>> FileDuplicates { get; }
        public IDataHolder<string, long> FileSizes { get; }

        protected string RootDir;
        protected bool CompletedScan = false;
        public delegate void ProcessFileDel(List<string> filepaths);


        /// <summary>
        /// Scan all folders recursively from root. Builds a dictinoary FileNames with key of file hash and value list of paths
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            Console.WriteLine("Start searching directories");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ProcessFileDel processFileDel = GetHashOfFiles;
            processFileDel += GetSizeOfFiles;

            var tasks = Walk(RootDir, processFileDel, true);
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
            stopwatch.Stop();
            ReportTime(stopwatch, "Walking dir took");

            FileDuplicates.Report();
            FileSizes.Report();
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
            int idx = 0;
            var pb = new ProgressBar(FileDuplicates.Data.Count);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach(var (key, val) in FileDuplicates.Data)
            {
                idx++;

                if (val.Count > 1)
                {
                    for(var i=1; i<val.Count; i++)
                    {
                        File.Delete(val[i]);
                    }
                   FileDuplicates.Data.AddOrUpdate(key, new List<string>(), (k, v) => { return new List<string>() { v[0] }; });
                }
                if (idx % 10 == 0)
                {
                    pb.Refresh(idx, "Deleting");
                } 
            }
            stopwatch.Stop();
            ReportTime(stopwatch, "Deleting duplicate files took");
            FileDuplicates.Report();
        }


        protected void ReportTime(Stopwatch stopwatch, string prefix)
        {
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format($"{prefix} {ts.Hours}:{ts.Minutes}:{ts.Seconds}.{ts.Milliseconds / 10}");
            Console.WriteLine(elapsedTime);
        }

        /// <summary>
        /// puts all files into the same flat directory
        /// </summary>
        public void Flatten(ConcurrentDictionary<string, List<string>> dict)
        {
            foreach(var item in dict.Values)
            {
                
            }

        }

        /// <summary>
        /// Walk folder recursively and add tasks to list to be awaited
        /// </summary>
        /// <param name="workDir"></param>
        /// <returns></returns>
        public List<Task> Walk(string workDir, ProcessFileDel processFileDel, bool displayProgressBar=false)
        {
            var files = FilterFiles(Directory.GetFiles(workDir));
            var tasks = new List<Task>();

            processFileDel(files);

            var folders = Directory.GetDirectories(workDir);

            var pb = displayProgressBar ? new ProgressBar(folders.Length) : null;
            int count = 0;
            foreach (var folder in folders)
            {
                foreach (var task in Walk(folder, processFileDel))
                {
                    tasks.Add(task);
                }
                if (pb != null)
                {
                    pb.Refresh(count++, folder);
                }
            }

            return tasks;
        }

        protected void GetHashOfFiles(List<string> files)
        {
            Parallel.ForEach(files, file =>
            {
                var fileHash = GetHashCRC32(file);
                FileDuplicates.Data.AddOrUpdate(fileHash, new List<string>() { file }, (k, v) => { v.Add(file); return v; });
            });
        }

        protected void GetSizeOfFiles(List<string> files)
        {
            Parallel.ForEach(files, file =>
            {
                var fileExt = Path.GetExtension(file);
                long size = new FileInfo(file).Length;
                FileSizes.Data.AddOrUpdate(fileExt, size, (k, v) => { return v + size; });
            });
        }
        
        protected virtual List<string> FilterFiles(string[] filenames)
        {
            return new List<string>(filenames);
        }

        /// <summary>
        /// Gets file hash MD5
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string GetHashMD5(string filepath)
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

        /// <summary>
        /// Gets file hash Crc-32
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string GetHashCRC32(string filepath)
        {
            var data = File.ReadAllBytes(filepath);
            var hash = Crc32.Hash(data);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
