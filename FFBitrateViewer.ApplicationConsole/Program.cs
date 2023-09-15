// See https://aka.ms/new-console-template for more information
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;

//var oSProcessService = new OSProcessService();
//await oSProcessService.ExecuteAsync(@"echo ""`env`""", standardOutputWriter: Console.Out);
//oSProcessService.Which("zsh").ToList().ForEach(Console.WriteLine);
//Console.WriteLine("Press any key to exit");

var ffprobeAppClient = new FFProbeAppClient();

//var version = await ffprobeProcessor.GetVersionAsync();
//Console.WriteLine($"ffprobe version:{version}");

var mediaFilePath = @"D:\documents\video\my-journey\NUCUMA\Nucuma-1.mkv";

//var mediaInfo = await ffprobeAppClient.GetMediaInfoAsync(mediaFilePath);
//Console.WriteLine($"ffprobe media info output :{mediaInfo}");

await foreach (var packet in ffprobeAppClient.GetMediaPackets(mediaFilePath))
{
    Console.WriteLine($"ffprobe media info output :{packet}");
}



