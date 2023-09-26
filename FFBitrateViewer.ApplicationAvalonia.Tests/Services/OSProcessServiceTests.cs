using FFBitrateViewer.ApplicationAvalonia.Services;
using System.Text;

namespace FFBitrateViewer.ApplicationAvalonia.Tests.Services
{
    internal class OSProcessServiceTests
    {
        private static readonly string TestAppFilePath = typeof(Program).Assembly.Location;

        private OSProcessService _OSProcessService;

        [SetUp]
        public void Setup()
        {
            _OSProcessService = new OSProcessService();
        }

        [Test]
        public async Task ExecuteCommandWithNoException()
        {
            // arrange
            var command = $"dotnet {TestAppFilePath} echo \"Hello World\"";

            // act
            var exitCode = await _OSProcessService.ExecuteAsync(command);

            // assert
            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        public async Task CaptureUnicodeInStandardOutputWriter()
        {
            // arrange
            var unicodeMessage = "Hello World 🌍 「世界、こんにちは。」";
            var command = $@"dotnet {TestAppFilePath} echo ""{unicodeMessage}""";

            // act
            var sb = new StringBuilder();
            var exitCode = await _OSProcessService.ExecuteAsync(command, standardOutputWriter: new StringWriter(sb));

            // assert
            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(sb.ToString(), Is.EqualTo(unicodeMessage));
        }

        [Test]
        public async Task ReturnsExitCode()
        {
            // arrange
            var command = $"dotnet {TestAppFilePath} exit 42";

            // act
            var exitCode = await _OSProcessService.ExecuteAsync(command);

            // assert
            Assert.That(exitCode, Is.EqualTo(42));
        }
    }
}
