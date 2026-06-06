namespace TphdCemuTrainer.ViewModels;

public sealed class ResearchReportViewModel
{
    public ResearchReportViewModel(
        string path,
        string fileName,
        string reportType,
        DateTime created,
        string captureA,
        string captureB,
        string changedBytes)
    {
        Path = path;
        FileName = fileName;
        ReportType = reportType;
        Created = created;
        CreatedText = created.ToString("g");
        CaptureA = captureA;
        CaptureB = captureB;
        ChangedBytes = changedBytes;
    }

    public string Path { get; }

    public string FileName { get; }

    public string ReportType { get; }

    public DateTime Created { get; }

    public string CreatedText { get; }

    public string CaptureA { get; }

    public string CaptureB { get; }

    public string ChangedBytes { get; }
}
