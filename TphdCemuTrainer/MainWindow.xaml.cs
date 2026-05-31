using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using TphdCemuTrainer.Cheats;
using TphdCemuTrainer.Memory;

namespace TphdCemuTrainer;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refreshTimer;
    private ProcessMemory? _memory;
    private ulong? _playerBaseAddress;
    private bool _isAttaching;
    private bool _isRefreshing;

    public MainWindow()
    {
        Cheats = new ObservableCollection<CheatRowViewModel>(
            CheatCatalog.All.Select(definition => new CheatRowViewModel(definition)));

        InitializeComponent();
        DataContext = this;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
    }

    public ObservableCollection<CheatRowViewModel> Cheats { get; }

    private async void AttachButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isAttaching)
        {
            return;
        }

        _isAttaching = true;
        SetControlsEnabled(false);
        SetStatus("Looking for Cemu.exe...", StatusKind.Working);

        try
        {
            Detach(clearStatus: false);

            if (!ProcessMemory.TryAttachToCemu(out var memory, out var attachError) || memory is null)
            {
                SetStatus(attachError, StatusKind.Error);
                return;
            }

            SetStatus("Attached to Cemu.exe. Scanning for TPHD player data...", StatusKind.Working);
            PidText.Text = memory.ProcessId.ToString(CultureInfo.InvariantCulture);

            var pattern = AobPattern.Parse(CheatCatalog.PlayerBaseAob);
            var scanner = new AobScanner();
            var foundAddress = await Task.Run(() => scanner.FindFirst(memory, pattern, CancellationToken.None));

            if (!foundAddress.HasValue)
            {
                memory.Dispose();
                PidText.Text = "-";
                PlayerBaseText.Text = "-";
                SetStatus("Cemu is open, but the TPHD player base AOB was not found. Load the game and a save, then rescan.", StatusKind.Error);
                return;
            }

            _memory = memory;
            _playerBaseAddress = foundAddress.Value;
            PlayerBaseText.Text = $"0x{foundAddress.Value:X}";
            DetachButton.IsEnabled = true;
            _refreshTimer.Start();

            SetStatus("Connected", StatusKind.Connected);
            RefreshCheats(applyLocks: false);
        }
        catch (Exception ex)
        {
            Detach(clearStatus: false);
            SetStatus($"Attach failed: {ex.Message}", StatusKind.Error);
        }
        finally
        {
            _isAttaching = false;
            SetControlsEnabled(true);
        }
    }

    private void DetachButton_Click(object sender, RoutedEventArgs e)
    {
        Detach(clearStatus: true);
    }

    private void SetCheat_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CheatRowViewModel row)
        {
            WriteRequestedValue(row);
        }
    }

    private void MaxCheat_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not CheatRowViewModel row)
        {
            return;
        }

        row.UseDefaultValue();
        WriteRequestedValue(row);
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        RefreshCheats(applyLocks: true);
    }

    private void RefreshCheats(bool applyLocks)
    {
        if (_isRefreshing)
        {
            return;
        }

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            return;
        }

        if (_memory.HasExited)
        {
            Detach(clearStatus: false);
            SetStatus("Cemu.exe exited.", StatusKind.Error);
            return;
        }

        _isRefreshing = true;
        try
        {
            foreach (var row in Cheats)
            {
                if (!TryReadCheat(row, out var currentValue, out var readError))
                {
                    SetStatus(readError, StatusKind.Error);
                    continue;
                }

                row.SetCurrentValue(currentValue);

                if (!applyLocks || !row.IsLocked || !row.CanLock)
                {
                    continue;
                }

                if (!row.TryGetDesiredValue(out var desiredValue, out _))
                {
                    continue;
                }

                // Cheat Engine's "Allow Increase" hotkeys keep a value from decreasing while letting gains stick.
                if (currentValue > desiredValue)
                {
                    row.DesiredValue = currentValue.ToString(CultureInfo.InvariantCulture);
                }
                else if (currentValue < desiredValue)
                {
                    WriteCheat(row, desiredValue, quiet: true);
                    row.SetCurrentValue(desiredValue);
                }
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private void WriteRequestedValue(CheatRowViewModel row)
    {
        if (!row.TryGetDesiredValue(out var desiredValue, out var validationError))
        {
            SetStatus(validationError, StatusKind.Error);
            return;
        }

        WriteCheat(row, desiredValue, quiet: false);
    }

    private bool TryReadCheat(CheatRowViewModel row, out int value, out string error)
    {
        value = 0;
        error = string.Empty;

        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            error = "Not attached to Cemu.exe.";
            return false;
        }

        var address = _playerBaseAddress.Value + row.Definition.Offset;
        switch (row.Definition.ValueKind)
        {
            case CheatValueKind.Byte:
                if (!_memory.TryReadBytes(address, 1, out var bytes, out var bytesRead) || bytesRead != 1)
                {
                    error = $"Could not read {row.Name} at 0x{address:X}.";
                    return false;
                }

                value = bytes[0];
                return true;

            case CheatValueKind.UInt16BigEndian:
                if (!BigEndianMemory.TryReadUInt16(_memory, address, out var beValue, out error))
                {
                    return false;
                }

                value = beValue;
                return true;

            default:
                error = $"Unsupported value type for {row.Name}.";
                return false;
        }
    }

    private bool WriteCheat(CheatRowViewModel row, int value, bool quiet)
    {
        if (_memory is null || !_playerBaseAddress.HasValue)
        {
            SetStatus("Not attached to Cemu.exe.", StatusKind.Error);
            return false;
        }

        var clampedValue = row.Definition.Clamp(value);
        var address = _playerBaseAddress.Value + row.Definition.Offset;
        string writeError;
        var writeSucceeded = row.Definition.ValueKind switch
        {
            CheatValueKind.Byte => _memory.TryWriteBytes(address, [(byte)clampedValue], out writeError),
            CheatValueKind.UInt16BigEndian => BigEndianMemory.TryWriteUInt16(_memory, address, (ushort)clampedValue, out writeError),
            _ => UnsupportedWrite(row, out writeError)
        };

        if (!writeSucceeded)
        {
            SetStatus(writeError, StatusKind.Error);
            return false;
        }

        row.SetCurrentValue(clampedValue);

        if (!quiet)
        {
            SetStatus($"{row.Name} set to {clampedValue}.", StatusKind.Connected);
        }

        return true;
    }

    private static bool UnsupportedWrite(CheatRowViewModel row, out string error)
    {
        error = $"Unsupported value type for {row.Name}.";
        return false;
    }

    private void Detach(bool clearStatus)
    {
        _refreshTimer.Stop();
        _memory?.Dispose();
        _memory = null;
        _playerBaseAddress = null;
        PidText.Text = "-";
        PlayerBaseText.Text = "-";
        DetachButton.IsEnabled = false;

        foreach (var row in Cheats)
        {
            row.MarkNotRead();
        }

        if (clearStatus)
        {
            SetStatus("Not attached", StatusKind.Neutral);
        }
    }

    private void SetControlsEnabled(bool enabled)
    {
        AttachButton.IsEnabled = enabled;
        CheatsGrid.IsEnabled = enabled;
    }

    private void SetStatus(string message, StatusKind kind)
    {
        StatusText.Text = message;
        StatusDot.Fill = kind switch
        {
            StatusKind.Connected => new SolidColorBrush(Color.FromRgb(24, 163, 98)),
            StatusKind.Error => new SolidColorBrush(Color.FromRgb(216, 71, 71)),
            StatusKind.Working => new SolidColorBrush(Color.FromRgb(224, 158, 54)),
            _ => new SolidColorBrush(Color.FromRgb(160, 168, 181))
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        Detach(clearStatus: false);
        base.OnClosed(e);
    }

    private enum StatusKind
    {
        Neutral,
        Working,
        Connected,
        Error
    }

}
