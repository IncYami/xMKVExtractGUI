using System.Threading.Tasks;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using xMKVExtractGUI.ViewModels;

namespace xMKVExtractGUI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not SettingsViewModel vm) return;

        // Wire browse folder dialog
        vm.BrowseFolderFunc = async title =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title         = title,
                AllowMultiple = false
            });
            return folders.Count > 0 ? folders[0].Path.LocalPath : null;
        };

        // Wire OK / Cancel buttons
        var okBtn     = this.FindControl<Button>("OkButton");
        var cancelBtn = this.FindControl<Button>("CancelButton");

        if (okBtn != null)
            okBtn.Click += (_, _) =>
            {
                if (DataContext is SettingsViewModel settingsVm)
                    settingsVm.ApplyToSettings();
                Close(true);
            };

        if (cancelBtn != null)
            cancelBtn.Click += (_, _) => Close(false);
    }
}
