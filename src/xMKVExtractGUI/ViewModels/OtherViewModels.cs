using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gMKVToolNix;
using gMKVToolNix.MkvExtract;
using xMKVExtractGUI.Models;
using xMKVExtractGUI.Services;

namespace xMKVExtractGUI.ViewModels;

// ═══════════════════════════════════════════════════════════════════════════════
//  LOG WINDOW
// ═══════════════════════════════════════════════════════════════════════════════
public partial class LogWindowViewModel : ObservableObject
{
    private readonly StringBuilder _sb = new();

    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _statusMessage = "";

    public void AppendLine(string line)
    {
        _sb.AppendLine(line);
        LogText = _sb.ToString();
    }

    [RelayCommand]
    private void ClearLog()
    {
        _sb.Clear();
        LogText = "";
        gMKVToolNix.Log.gMKVLogger.Clear();
        StatusMessage = "Log cleared.";
    }

    [RelayCommand]
    private async Task SaveLog()
    {
        WeakReferenceMessenger.Default.Send(new SaveLogMessage(LogText));
        await Task.CompletedTask;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  SETTINGS WINDOW  (combines MKVToolNix path + filename patterns + advanced)
// ═══════════════════════════════════════════════════════════════════════════════
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    // MKVToolNix path
    [ObservableProperty] private string _mkvToolNixPath = "";

    // Filename patterns
    [ObservableProperty] private string _videoTrackPattern    = "";
    [ObservableProperty] private string _audioTrackPattern    = "";
    [ObservableProperty] private string _subtitleTrackPattern = "";
    [ObservableProperty] private string _chapterPattern       = "";
    [ObservableProperty] private string _attachmentPattern    = "";
    [ObservableProperty] private string _tagsPattern          = "";

    // Advanced
    [ObservableProperty] private bool _disableBom;
    [ObservableProperty] private bool _useRawExtraction;
    [ObservableProperty] private bool _useFullRawExtraction;

    [ObservableProperty] private string _statusMessage = "";

    // Injected by View
    public Func<string, Task<string?>>? BrowseFolderFunc { get; set; }

    public string PlaceholdersInfo { get; } = """
        Common:  {FilenameNoExt}  {Filename}  {DirSeparator}
        Track:   {TrackNumber}  {TrackNumber:0}  {TrackNumber:00}  {TrackNumber:000}
                 {TrackID}  {TrackID:0}  {TrackID:00}  {TrackID:000}
                 {TrackName}  {Language}  {LanguageIETF}  {CodecID}
                 {Delay}  {EffectiveDelay}  {TrackForced}
        Video:   {PixelWidth}  {PixelHeight}
        Audio:   {SamplingFrequency}  {Channels}
        Attach:  {AttachmentID}  {AttachmentFilename}  {MimeType}  {AttachmentFileSize}
        """;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        MkvToolNixPath        = _settings.MkvToolNixPath;
        VideoTrackPattern     = _settings.VideoTrackFilenamePattern;
        AudioTrackPattern     = _settings.AudioTrackFilenamePattern;
        SubtitleTrackPattern  = _settings.SubtitleTrackFilenamePattern;
        ChapterPattern        = _settings.ChapterFilenamePattern;
        AttachmentPattern     = _settings.AttachmentFilenamePattern;
        TagsPattern           = _settings.TagsFilenamePattern;
        DisableBom            = _settings.DisableBomForTextFiles;
        UseRawExtraction      = _settings.UseRawExtraction;
        UseFullRawExtraction  = _settings.UseFullRawExtraction;
    }

    [RelayCommand]
    private async Task BrowseMkvToolNixPath()
    {
        if (BrowseFolderFunc == null) return;
        var dir = await BrowseFolderFunc("Select MKVToolNix Directory");
        if (dir != null)
        {
            MkvToolNixPath = dir;
            StatusMessage  = $"Path set to: {dir}";
        }
    }

    [RelayCommand]
    private void AutoDetect()
    {
        try
        {
            string found = "";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                found = gMKVHelper.GetMKVToolnixPathViaRegistry();
            }
            else
            {
                var paths = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (var p in paths.Split(Path.PathSeparator))
                {
                    if (File.Exists(Path.Combine(p, "mkvmerge")))
                    {
                        found = p;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(found))
            {
                MkvToolNixPath = found;
                StatusMessage  = $"Auto-detected: {found}";
            }
            else
            {
                StatusMessage = "Auto-detect failed: 'mkvmerge' not found in PATH.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Auto-detect error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        VideoTrackPattern    = "{FilenameNoExt}_track{TrackNumber:00}_{Language}";
        AudioTrackPattern    = "{FilenameNoExt}_track{TrackNumber:00}_{Language}";
        SubtitleTrackPattern = "{FilenameNoExt}_track{TrackNumber:00}_{Language}";
        ChapterPattern       = "{FilenameNoExt}_chapters";
        AttachmentPattern    = "{AttachmentFilename}";
        TagsPattern          = "{FilenameNoExt}_tags";
        StatusMessage        = "All patterns reset to defaults.";
    }

    public void ApplyToSettings()
    {
        _settings.MkvToolNixPath                 = MkvToolNixPath;
        _settings.VideoTrackFilenamePattern       = VideoTrackPattern;
        _settings.AudioTrackFilenamePattern       = AudioTrackPattern;
        _settings.SubtitleTrackFilenamePattern    = SubtitleTrackPattern;
        _settings.ChapterFilenamePattern          = ChapterPattern;
        _settings.AttachmentFilenamePattern       = AttachmentPattern;
        _settings.TagsFilenamePattern             = TagsPattern;
        _settings.DisableBomForTextFiles          = DisableBom;
        _settings.UseRawExtraction                = UseRawExtraction;
        _settings.UseFullRawExtraction            = UseFullRawExtraction;
        SettingsService.Save(_settings);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  ABOUT WINDOW
// ═══════════════════════════════════════════════════════════════════════════════
public partial class AboutViewModel : ObservableObject
{
    public string AppName { get; }
    public string Version { get; }
    public string Author { get; }
    public string Description { get; }
    public string GitHubUrl { get; }

    public AboutViewModel()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var nameData = assembly.GetName();

        AppName = nameData.Name ?? "xMKVExtractGUI";
        Version = $"v{nameData.Version?.ToString(3)}";
        Author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "Unknown";
        Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";
        var attributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>();
        GitHubUrl = attributes.FirstOrDefault(a => a.Key == "RepositoryUrl")?.Value 
                    ?? "https://github.com/IncYami/xMKVExtractGUI";
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  JOB MANAGER WINDOW
// ═══════════════════════════════════════════════════════════════════════════════
public partial class JobManagerViewModel : ObservableObject
{
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty] private int    _currentProgress;
    [ObservableProperty] private int    _totalProgress;
    [ObservableProperty] private string _currentTrackName = "";
    [ObservableProperty] private string _statusMessage    = "";
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private bool   _popupNotifications = true;

    public System.Collections.ObjectModel.ObservableCollection<JobItem> Jobs => _mainVm.Jobs;

    // Injected by View for file dialogs
    public Func<string, string[], Task<string?>>? SaveFileFunc  { get; set; }
    public Func<string, string[], Task<string?>>? OpenFileFunc  { get; set; }

    private gMKVExtract?            _extractor;
    private CancellationTokenSource? _cts;

    public JobManagerViewModel(MainWindowViewModel mainVm)
    {
        _mainVm = mainVm;
    }

    [RelayCommand]
    private async Task RunJobs()
    {
        var readyJobs = Jobs.Where(j => j.IsSelected && j.Status == "Ready").ToList();
        if (!readyJobs.Any()) { StatusMessage = "No ready jobs selected."; return; }

        IsRunning  = true;
        _cts       = new CancellationTokenSource();
        _extractor = new gMKVExtract(_mainVm._settings.MkvToolNixPath);

        _extractor.MkvExtractProgressUpdated += p => CurrentProgress = p;
        _extractor.MkvExtractTrackUpdated    += (_, name) =>
        {
            CurrentTrackName = name;
            StatusMessage    = $"Extracting: {name}";
        };

        int done = 0;
        try
        {
            foreach (var job in readyJobs)
            {
                if (_cts.Token.IsCancellationRequested) break;
                if (job.Parameters == null) continue;

                job.Status = "Running";
                await Task.Run(() => _extractor.ExtractMKVSegmentsThreaded(job.Parameters), _cts.Token);

                if (_extractor.ThreadedException != null)
                {
                    job.Status    = $"Error: {_extractor.ThreadedException.Message}";
                    StatusMessage = job.Status;
                }
                else
                {
                    job.Status   = "Done";
                    job.Progress = 100;
                }

                done++;
                TotalProgress = (int)((double)done / readyJobs.Count * 100);
            }

            StatusMessage = "All jobs completed.";
            if (PopupNotifications)
                WeakReferenceMessenger.Default.Send(new ShowNotificationMessage("Jobs Complete",
                    $"{done} job(s) finished."));
        }
        catch (OperationCanceledException) { StatusMessage = "Jobs aborted."; }
        catch (Exception ex)               { StatusMessage = $"Job error: {ex.Message}"; }
        finally
        {
            IsRunning  = false;
            _extractor = null;
        }
    }

    [RelayCommand] private void Abort()    { if (_extractor != null) _extractor.Abort = true; }
    [RelayCommand] private void AbortAll()
    {
        if (_extractor != null) { _extractor.Abort = true; _extractor.AbortAll = true; }
        _cts?.Cancel();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        var toRemove = Jobs.Where(j => j.IsSelected).ToList();
        foreach (var j in toRemove) Jobs.Remove(j);
    }

    [RelayCommand] private void SelectAll()   => Jobs.ToList().ForEach(j => j.IsSelected = true);
    [RelayCommand] private void DeselectAll() => Jobs.ToList().ForEach(j => j.IsSelected = false);

    [RelayCommand]
    private void ResetSelected()
    {
        foreach (var j in Jobs.Where(j => j.IsSelected))
        {
            j.Status   = "Ready";
            j.Progress = 0;
        }
    }

    [RelayCommand]
    private async Task SaveJobs()
    {
        if (SaveFileFunc == null) return;
        var path = await SaveFileFunc("Save Jobs", ["*.json"]);
        if (path == null) return;
        try
        {
            var list = Jobs.Select(j => new
            {
                j.SourceFile,
                j.OutputDirectory,
                j.Tracks,
                j.Status
            });
            var json = System.Text.Json.JsonSerializer.Serialize(list,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            StatusMessage = $"Jobs saved to: {Path.GetFileName(path)}";
        }
        catch (Exception ex) { StatusMessage = $"Save error: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task LoadJobs()
    {
        if (OpenFileFunc == null) return;
        var path = await OpenFileFunc("Load Jobs", ["*.json"]);
        if (path == null || !File.Exists(path)) return;
        StatusMessage = $"Loaded: {Path.GetFileName(path)} (reload not fully supported — add files via main window).";
    }
}
