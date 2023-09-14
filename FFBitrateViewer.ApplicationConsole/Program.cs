// See https://aka.ms/new-console-template for more information
using FFBitrateViewer.ApplicationAvalonia.Services;

//var oSProcessService = new OSProcessService();
//await oSProcessService.ExecuteAsync(@"echo ""`env`""", standardOutputWriter: Console.Out);
//oSProcessService.Which("zsh").ToList().ForEach(Console.WriteLine);
//Console.WriteLine("Press any key to exit");

var ffprobeProcessor = new FFProbeProcessor();
var version = await ffprobeProcessor.GetVersion();
Console.WriteLine($"ffprobe version:{version}");
//Console.ReadLine();

