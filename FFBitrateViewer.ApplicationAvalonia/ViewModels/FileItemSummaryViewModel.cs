using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace FFBitrateViewer.ApplicationAvalonia.ViewModels;

public partial class FileItemSummaryViewModel : ViewModelBase
{
    public const string CategoryNameFile = "File";

    [property: Category(CategoryNameFile), ReadOnly(true), DisplayName("Video"), Description("Video stream count")]
    public int VideoStreamCount { get; set; }

    [property: Category(CategoryNameFile), ReadOnly(true), DisplayName("Audio"), Description("Audio stream count")]
    public int AudioStreamCount { get; set; }

    [property: Category(CategoryNameFile), ReadOnly(true), DisplayName("Subtitle"), Description("Subtitle stream count")]
    public int SubtitleStreamCount { get; set; }

    [property: Category(CategoryNameFile), ReadOnly(true), DisplayName("Start"), Description("File start time")]
    public string FileStart { get; set; } = string.Empty;

    [property: Category(CategoryNameFile), ReadOnly(true), DisplayName("Duration"), Description("File duration")]
    public string FileDuration { get; set; } = string.Empty;

    [property: Category(CategoryNameFile), ReadOnly(true), DisplayName("Bit rate"), Description("File bit rate")]
    public string FileBitRate { get; set; } = string.Empty;


    public const string CategoryNameVideoStream = "Video Stream";

    [property: Category(CategoryNameVideoStream), ReadOnly(true), DisplayName("Frames count"), Description("Video stream frames count")]
    [ObservableProperty]
    private int _frameCount;

    [property: Category(CategoryNameVideoStream), ReadOnly(true), DisplayName("Frame rate"), Description("Video stream frame rate")]
    public string FrameRate { get; set; } = string.Empty;

    [property: Category(CategoryNameVideoStream), ReadOnly(true), DisplayName("Start"), Description("Video stream start")]
    public string VideoStart { get; set; } = string.Empty;

    [property: Category(CategoryNameVideoStream), ReadOnly(true), DisplayName("Duration"), Description("Video stream duration")]
    public string VideoStreamDuration { get; set; } = string.Empty;

    [property: Category(CategoryNameVideoStream), ReadOnly(true), DisplayName("Bit rate"), Description("Video bit rate")]
    public string VideoBitRate { get; set; } = string.Empty;


    public const string CategoryNameFrame = "Frame";

    [property: Category(CategoryNameFrame), ReadOnly(true), DisplayName("Size"), Description("Frame size")]
    public string Size { get; set; } = string.Empty;

    [property: Category(CategoryNameFrame), ReadOnly(true), DisplayName("Field type"), Description("Frame field type")]
    public string FileType { get; set; } = string.Empty;

    [property: Category(CategoryNameFrame), ReadOnly(true), DisplayName("Color space"), Description("Frame color space")]
    public string ColorSpace { get; set; } = string.Empty;

    [property: Category(CategoryNameFrame), ReadOnly(true), DisplayName("Color range"), Description("Frame color range")]
    public string ColorRange { get; set; } = string.Empty;

}