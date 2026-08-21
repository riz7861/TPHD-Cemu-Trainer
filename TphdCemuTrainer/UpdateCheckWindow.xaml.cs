using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using TphdCemuTrainer.Updates;

namespace TphdCemuTrainer;

public partial class UpdateCheckWindow : Window
{
    private static readonly string[] ThemeResourceKeys =
    [
        "AppBackgroundBrush",
        "PanelBackgroundBrush",
        "ControlBackgroundBrush",
        "TextBrush",
        "SecondaryTextBrush",
        "BorderBrush",
        "ControlHoverBackgroundBrush",
        "ControlFocusedBorderBrush",
        "DisabledTextBrush",
        "DisabledControlBackgroundBrush"
    ];

    private readonly UpdateCheckResult _result;

    public UpdateCheckWindow(UpdateCheckResult result, ResourceDictionary themeResources)
    {
        _result = result;

        InitializeComponent();
        CopyThemeResources(themeResources);
        ApplyResult();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ViewUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (!GitHubUpdateService.IsSafeGitHubReleaseUrl(_result.ReleaseUri))
        {
            MessageTextBlock.Text = "Unable to open the update link.";
            DetailTextBlock.Text = "The release link could not be verified.";
            ViewUpdateButton.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_result.ReleaseUri!.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            MessageTextBlock.Text = "Unable to open the update link.";
            DetailTextBlock.Text = "Please visit the TPHD Cemu Trainer GitHub releases page manually.";
        }
    }

    private void ApplyResult()
    {
        switch (_result.State)
        {
            case UpdateCheckState.UpdateAvailable:
                MessageTextBlock.Text = $"Version {_result.LatestVersionText} is available.";
                DetailTextBlock.Text = "Open the official GitHub release page to view the update.";
                ViewUpdateButton.Visibility = Visibility.Visible;
                break;
            case UpdateCheckState.Current:
                MessageTextBlock.Text = "You're up to date.";
                DetailTextBlock.Text = "No newer stable GitHub release was found.";
                break;
            default:
                MessageTextBlock.Text = "Unable to check for updates right now.";
                DetailTextBlock.Text = "The trainer will continue to work normally.";
                break;
        }
    }

    private void CopyThemeResources(ResourceDictionary source)
    {
        foreach (var key in ThemeResourceKeys)
        {
            if (!source.Contains(key))
            {
                continue;
            }

            Resources[key] = source[key] is Freezable freezable
                ? freezable.CloneCurrentValue()
                : source[key];
        }
    }
}
