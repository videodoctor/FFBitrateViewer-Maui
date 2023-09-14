// See https://aka.ms/new-console-template for more information
using FFBitrateViewer.ApplicationAvalonia.Services;

var oSProcessService = new OSProcessService();
await oSProcessService.ExecuteAsync(@"echo $env:PATH;", standardOutputWriter: Console.Out);
//Console.WriteLine("Hello, World!");
