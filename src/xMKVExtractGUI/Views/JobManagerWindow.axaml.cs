using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using xMKVExtractGUI.ViewModels;

namespace xMKVExtractGUI.Views;

public partial class JobManagerWindow : Window
{
    public JobManagerWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not JobManagerViewModel vm) return;

        vm.SaveFileFunc = async (title, filters) =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title            = title,
                SuggestedFileName = "jobs.json",
                DefaultExtension  = "json",
                FileTypeChoices   =
                [
                    new FilePickerFileType("JSON") { Patterns = ["*.json"] },
                    new FilePickerFileType("All")  { Patterns = ["*.*"] }
                ]
            });
            return file?.Path.LocalPath;
        };

        vm.OpenFileFunc = async (title, filters) =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title         = title,
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON") { Patterns = ["*.json"] },
                    new FilePickerFileType("All")  { Patterns = ["*.*"] }
                ]
            });
            return files.Count > 0 ? files[0].Path.LocalPath : null;
        };
    }
}
