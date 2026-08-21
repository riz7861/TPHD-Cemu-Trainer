using System.Windows;
using System.Windows.Media;
using TphdCemuTrainer.Credits;

namespace TphdCemuTrainer;

public partial class AboutWindow : Window
{
    private static readonly string[] ThemeResourceKeys =
    [
        "AppBackgroundBrush",
        "PanelBackgroundBrush",
        "ControlBackgroundBrush",
        "TextBrush",
        "SecondaryTextBrush",
        "BorderBrush",
        "WarningBackgroundBrush",
        "WarningBorderBrush",
        "WarningTextBrush",
        "ControlHoverBackgroundBrush",
        "ControlFocusedBorderBrush",
        "DisabledTextBrush",
        "DisabledControlBackgroundBrush"
    ];

    public AboutWindow(string applicationTitle, ResourceDictionary themeResources)
    {
        ApplicationTitle = applicationTitle;

        InitializeComponent();
        CopyThemeResources(themeResources);
        DataContext = this;
    }

    public string ApplicationTitle { get; }

    public IReadOnlyList<CreditEntry> CreditEntries => CreditsCatalog.Entries;

    public string CreditsAcknowledgement => CreditsCatalog.Acknowledgement;

    public string CreditsMissingContributorNotice => CreditsCatalog.MissingContributorNotice;

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
