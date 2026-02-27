using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using xMKVExtractGUI.ViewModels;

namespace xMKVExtractGUI.Views;

public partial class LogWindow : Window
{
    public LogWindow()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var copyBtn    = this.FindControl<Button>("CopySelectionBtn");
        var refreshBtn = this.FindControl<Button>("RefreshBtn");
        var closeBtn   = this.FindControl<Button>("CloseBtn");
        var logBox     = this.FindControl<TextBox>("LogTextBox");

        // Copy selection to clipboard
        if (copyBtn != null && logBox != null)
            copyBtn.Click += (_, _) =>
            {
                var text = logBox.SelectedText;
                if (!string.IsNullOrEmpty(text))
                    Clipboard?.SetTextAsync(text);
            };

        // Refresh — scrolls to end
        if (refreshBtn != null && logBox != null)
            refreshBtn.Click += (_, _) =>
            {
                logBox.CaretIndex = logBox.Text?.Length ?? 0;
            };

        // Close
        if (closeBtn != null)
            closeBtn.Click += (_, _) => Close();

        // Wire Save via message bus
        WeakReferenceMessenger.Default.Register<SaveLogMessage>(this, async msg =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title           = "Save Log",
                SuggestedFileName = "xMKVExtractGUI.log",
                DefaultExtension = "log",
                FileTypeChoices  =
                [
                    new FilePickerFileType("Log Files") { Patterns = ["*.log", "*.txt"] },
                    new FilePickerFileType("All Files")  { Patterns = ["*.*"] }
                ]
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(msg.LogContent);
                if (DataContext is LogWindowViewModel vm)
                    vm.StatusMessage = $"Log saved to: {file.Name}";
            }
        });
    }
}
