using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using xMKVExtractGUI.ViewModels;

namespace xMKVExtractGUI.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<OpenSettingsMessage>(this, msg => OpenSettings(msg.Settings));
        WeakReferenceMessenger.Default.Register<OpenAboutMessage>(this, _ => OpenAbout());
        WeakReferenceMessenger.Default.Register<OpenLogsMessage>(this, msg => OpenLogs(msg.LogViewModel));
        WeakReferenceMessenger.Default.Register<OpenJobManagerMessage>(this, msg => OpenJobManager(msg.MainVm));

#pragma warning disable CS0618
        AddHandler(DragDrop.DropEvent, OnDrop);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
#pragma warning restore CS0618
        
        DragDrop.SetAllowDrop(this, true);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            _vm = vm;
            vm.BrowseFolderFunc = BrowseFolderAsync;
            vm.PickFilesFunc = PickFilesAsync;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm?.SaveSettings();
        base.OnClosed(e);
    }

    // ─── Drag and Drop ────────────────────────────────────────────────

#pragma warning disable CS0618 
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var hasFiles = e.Data.GetFiles()?.Any() == true;
        
        e.DragEffects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (_vm == null) return;

        var files = e.Data.GetFiles()?.ToList() ?? [];
        var validPaths = new List<string>();

        foreach (var file in files)
        {
            if (file is IStorageFile sf)
            {
                var path = sf.Path.LocalPath;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext is ".mkv" or ".mka" or ".mks" or ".mk3d" or ".webm")
                {
                    validPaths.Add(path);
                }
            }
        }

        if (validPaths.Any())
        {
            await _vm.LoadFiles(validPaths);
        }
    }
#pragma warning restore CS0618

    // ─── Dialog helpers ───────────────────────────────────────────────

    private async Task<string?> BrowseFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });
        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private async Task<List<string>> PickFilesAsync(string title, string[] filters)
    {
        var patterns = filters.ToList();
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("Matroska Files")
                {
                    Patterns = patterns
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            ]
        });
        return files.Select(f => f.Path.LocalPath).ToList();
    }

    // ─── Window actions ───────────────────────────────────────────────

    private void OpenSettings(xMKVExtractGUI.Services.AppSettings settings)
    {
        var vm = new SettingsViewModel(settings);
        var window = new SettingsWindow { DataContext = vm };
        window.ShowDialog(this);
    }

    private void OpenAbout()
    {
        var vm = new AboutViewModel();
        var window = new AboutWindow { DataContext = vm };
        window.ShowDialog(this);
    }

    private void OpenLogs(LogWindowViewModel logVm)
    {
        var window = new LogWindow { DataContext = logVm };
        window.Show(this);
    }

    private void OpenJobManager(MainWindowViewModel mainVm)
    {
        var vm = new JobManagerViewModel(mainVm);
        var window = new JobManagerWindow { DataContext = vm };
        window.Show(this);
    }
}