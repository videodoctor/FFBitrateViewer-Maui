// See https://aka.ms/new-console-template for more information
using FFBitrateViewer.ApplicationAvalonia.Services;
using FFBitrateViewer.ApplicationAvalonia.Services.ffprobe;

var oSProcessService = new OSProcessService();
await oSProcessService.ExecuteAsync(@"echo ""`env`""", standardOutputWriter: Console.Out);
oSProcessService.Which("pwsh.exe").ToList().ForEach(Console.WriteLine);

var ffprobeAppClient = new FFProbeAppClient();
var mediaFilePath = @"D:\documents\video\my-journey\NUCUMA\Nucuma-1.mkv";

var version = await ffprobeAppClient.GetVersionAsync();
Console.WriteLine($"ffprobe version:{version}");

var mediaInfo = await ffprobeAppClient.GetMediaInfoAsync(mediaFilePath);
Console.WriteLine($"ffprobe media info output :{mediaInfo}");

//await foreach (var packet in ffprobeAppClient.GetProbePackets(mediaFilePath))
//{
//    Console.WriteLine($"PtsTime:{packet.PtsTime} DtsTime:{packet.DtsTime} DurationTime:{packet.DurationTime} Size:{packet.Size} Flags:{packet.Flags}");
//}


Console.WriteLine("Press any key to exit");
