// See https://aka.ms/new-console-template for more information
using PhotoDedupe;

Console.WriteLine("Hello, World!");
var path = "C:\\Users\\Oliver\\source\\repos\\PhotoDedupe\\test_folder";

var runner = new ImageManager(path);
//await runner.FindSimilarImages();
//await runner.RemoveDeleted(runner.FileNames);
runner.Run();
