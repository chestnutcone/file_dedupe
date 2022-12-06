using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using PhotoDedupe.Data;

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
            ImageSimilarity = new ImageSimilarity();
        }

        public IDataHolder<string, List<string>> ImageSimilarity { get; }

        protected override List<string> FilterFiles(string[] filenames)
        {
            var filteredList = new List<string>();
            foreach (string file in filenames)
            {
                if (ExtAllowed(file))
                {
                    filteredList.Add(file);
                }
            }
            return filteredList;
        }

        private bool ExtAllowed(string filename)
        {
            var allowedExt = new HashSet<string>(){ ".jpeg", ".jpg", ".png" };
            var fileExt = Path.GetExtension(filename);
            return allowedExt.Contains(fileExt.ToLower());
        }

        public async Task FindSimilarImages()
        {
            if (!CompletedScan)
            {
                await Run();
            }
            
            // build hash for every image available
            foreach(var files in FileHashDuplicates.Data.Values)
            {
                var hash = BrightnessHash(files[0]);
                ImageSimilarity.Data.AddOrUpdate(hash, files, (k, v) => {
                    foreach (var file in files)
                    {
                        v.Add(file);
                    }
                    return v;
                });
            }
            Console.WriteLine("Similar Images");
            ImageSimilarity.Report();
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
