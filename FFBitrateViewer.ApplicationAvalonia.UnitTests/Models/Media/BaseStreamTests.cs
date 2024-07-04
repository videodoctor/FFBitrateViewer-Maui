using FFBitrateViewer.ApplicationAvalonia.Models.Media;
using FFBitrateViewer.ApplicationAvalonia.Services.FFProbe;

namespace FFBitrateViewer.ApplicationAvalonia.UnitTests.Models.Media;

public class BaseStreamTests
{
    [Fact]
    public void BuildWithFFProbeStreamNullShouldThrowException()
    {
        // Arrange
        FFProbeStream? probeStream = null;

        // Act
        var exception = Record.Exception(() => AudioStream.Build(probeStream!));

        // Assert
        Assert.NotNull(exception);
        Assert.IsType<ArgumentNullException>(exception);
    }

}