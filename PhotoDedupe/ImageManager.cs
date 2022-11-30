using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace PhotoDedupe
{
    internal class ImageManager : FolderManager
    {
        /// <summary>
        /// Only supports on windows because of System.Drawing
        /// </summary>
        /// <param name="rootDir"></param>
        public ImageManager(string rootDir) : base(rootDir)
        {
        }

        public ConcurrentDictionary<string, List<string>> SimilarityHash = new ConcurrentDictionary<string, List<string>>();

        protected override List<string> FilterFiles(string[] filenames)
        {
            var filteredList = new List<string>();
            foreach (string file in filenames)
            {
                if (file.EndsWith(".jpeg") || file.EndsWith(".jpg"))
                {
                    filteredList.Add(file);
                }
            }
            return filteredList;
        }

        public async Task FindSimilarImages()
        {
            if (!CompletedScan)
            {
                await Run();
            }
            
            // build hash for every image available
            foreach(var files in FileNames.Values)
            {
                var hash = BrightnessHash(files[0]);
                SimilarityHash.AddOrUpdate(hash, files, (k, v) => {
                    foreach (var file in files)
                    {
                        v.Add(file);
                    }
                    return v;
                });
            }
            Console.WriteLine("Similar Images");
            Report(SimilarityHash);
        }

        /// <summary>
        /// Downsize and use pixel brightness and cutoff as hash
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public string BrightnessHash(string imagePath)
        {
            List<string> result = new List<string>();

            var image = Image.FromFile(imagePath);
            var bitmap = new Bitmap(image, 16, 16);
            for (int i=0; i<bitmap.Width; i++)
            {
                for(int j=0; j<bitmap.Height; j++)
                {
                    result.Add(bitmap.GetPixel(i, j).GetBrightness() < 0.5f ? "1": "0") ;
                }
            }
            return string.Join("", result);
        }
    }
}
