// See https://aka.ms/new-console-template for more information
using PhotoDedupe;

Console.WriteLine("Hello, World!");
var path = "F:\\Canada Photos";

var runner = new ImageManager(path);
//await runner.FindSimilarImages();
//await runner.RemoveDeleted(runner.FileNames);
await runner.Run();
