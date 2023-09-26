using FFBitrateViewer.ApplicationAvalonia.Services;

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
