using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gMKVToolNix;
using gMKVToolNix.MkvExtract;
using gMKVToolNix.MkvMerge;
using gMKVToolNix.Segments;
using xMKVExtractGUI.Models;
using xMKVExtractGUI.Services;

namespace xMKVExtractGUI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    // ─── Internal state ───────────────────────────────────────────────────────
    internal readonly AppSettings _settings;
    private gMKVExtract? _extractor;
    private CancellationTokenSource? _cts;

    // ─── File-dialog delegates (injected by View) ─────────────────────────────
    public Func<string, Task<string?>>? BrowseFolderFunc { get; set; }
    public Func<string, string[], Task<List<string>>>? PickFilesFunc { get; set; }

    // ─── Observable properties ────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<MkvFileNode> _fileNodes = [];
    [ObservableProperty] private MkvFileNode? _selectedFileNode;
    [ObservableProperty] private string _selectedFileInfo = "";
    [ObservableProperty] private bool _appendOnDragDrop;
    [ObservableProperty] private bool _overwriteExistingFiles;
    [ObservableProperty] private string _outputDirectory = "";
    [ObservableProperty] private bool _useSourceDirectory;
    [ObservableProperty] private bool _popupNotifications = true;
    [ObservableProperty] private string _selectedChapterType = "XML";
    [ObservableProperty] private string _selectedExtractionMode = "Tracks";
    [ObservableProperty] private int _currentProgress;
    [ObservableProperty] private int _totalProgress;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isExtracting;
    [ObservableProperty] private string _expandToggleText = "▲ Collapse All";

    // ─── Track-type bulk-select properties ────────────────────────────────────
    // Nullable bool: true = all checked, false = none checked, null = mixed
    [ObservableProperty] private bool? _allTracksChecked = false;
    [ObservableProperty] private bool? _allVideoChecked = false;
    [ObservableProperty] private bool? _allAudioChecked = false;
    [ObservableProperty] private bool? _allSubtitlesChecked = false;
    [ObservableProperty] private bool? _allChaptersChecked = false;
    [ObservableProperty] private bool? _allAttachmentsChecked = false;

    // ─── Static lists ─────────────────────────────────────────────────────────
    public List<string> ChapterTypes    { get; } = ["XML", "OGM", "CUE", "PBF"];
    public List<string> ExtractionModes { get; } =
        ["Tracks", "Tags", "Attachments", "Chapters", "Cue_Sheet", "Timecodes_v2", "Timestamps_v2", "Cues"];

    // ─── Child collections & sub-view-models ──────────────────────────────────
    public ObservableCollection<JobItem> Jobs { get; } = [];
    public LogWindowViewModel LogViewModel { get; } = new();

    // ─── Constructor ──────────────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        _settings = SettingsService.Load();
        OutputDirectory         = _settings.LastOutputDirectory;
        UseSourceDirectory      = _settings.UseSourceDirectory;
        AppendOnDragDrop        = _settings.AppendOnDragDrop;
        OverwriteExistingFiles  = _settings.OverwriteExistingFiles;
        PopupNotifications      = _settings.PopupNotifications;

        gMKVToolNix.Log.gMKVLogger.LogLineAdded += (line, _) => LogViewModel.AppendLine(line);
    }

    // ─── File Management ──────────────────────────────────────────────────────

    [RelayCommand]
    public async Task AddInputFile()
    {
        if (PickFilesFunc == null) return;
        var files = await PickFilesFunc("Select MKV Files", ["*.mkv", "*.mka", "*.mks", "*.mk3d", "*.webm"]);
        await LoadFiles(files);
    }

    public async Task LoadFiles(IEnumerable<string> filePaths)
    {
        var paths = filePaths.ToList();
        if (paths.Count == 0) return;

        if (!AppendOnDragDrop && FileNodes.Any())
        {
            FileNodes.Clear();
            SelectedFileNode = null;
            SelectedFileInfo = "";
        }

        foreach (var path in paths)
        {
            await LoadSingleFile(path);
        }
    }

    public async Task LoadSingleFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
 
        if (FileNodes.Any(n => n.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            return;

        try
        {
            StatusMessage = $"Loading: {Path.GetFileName(filePath)}…";

            var mkvMerge = new gMKVMerge(_settings.MkvToolNixPath);
            var segments = await Task.Run(() => mkvMerge.GetMKVSegments(filePath));

            var fileNode = new MkvFileNode { FilePath = filePath };
            fileNode.PropertyChanged += OnFileNodePropertyChanged;

            foreach (var seg in segments)
            {
                switch (seg)
                {
                    case gMKVSegmentInfo info:
                        fileNode.SegmentInfo = info;
                        break;
                    case gMKVTrack track:
                        var trackNode = new TrackNode
                        {
                            Segment     = track,
                            DisplayText = track.ToString(),
                            NodeType    = track.TrackType switch
                            {
                                MkvTrackType.video     => TrackNodeType.Video,
                                MkvTrackType.audio     => TrackNodeType.Audio,
                                MkvTrackType.subtitles => TrackNodeType.Subtitle,
                                _                      => TrackNodeType.Unknown
                            },
                            IsChecked = false
                        };
                        trackNode.PropertyChanged += OnTrackNodePropertyChanged;
                        fileNode.Tracks.Add(trackNode);
                        break;
                    case gMKVAttachment att:
                        var attNode = new TrackNode
                        {
                            Segment     = att,
                            DisplayText = att.ToString(),
                            NodeType    = TrackNodeType.Attachment,
                            IsChecked   = false
                        };
                        attNode.PropertyChanged += OnTrackNodePropertyChanged;
                        fileNode.Tracks.Add(attNode);
                        break;
                    case gMKVChapter chapter:
                        var chapNode = new TrackNode
                        {
                            Segment     = chapter,
                            DisplayText = chapter.ToString(),
                            NodeType    = TrackNodeType.Chapter,
                            IsChecked   = false
                        };
                        chapNode.PropertyChanged += OnTrackNodePropertyChanged;
                        fileNode.Tracks.Add(chapNode);
                        break;
                }
            }

            FileNodes.Add(fileNode);
            SelectedFileNode = fileNode;
            RefreshCheckStates();
            StatusMessage = $"Loaded: {Path.GetFileName(filePath)} ({fileNode.Tracks.Count} tracks/segments)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
        }
    }

    private void OnTrackNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TrackNode.IsChecked))
            RefreshCheckStates();
    }

    private bool _updatingFileCheck;

    private void OnFileNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MkvFileNode.IsChecked) && !_updatingFileCheck)
        {
            if (sender is MkvFileNode file)
            {
                _updatingFileCheck = true;
                bool target = file.IsChecked ?? false;
                foreach (var track in file.Tracks)
                {
                    track.IsChecked = target;
                }
                _updatingFileCheck = false;
                RefreshCheckStates();
            }
        }
    }

    [RelayCommand]
    private void RemoveSelectedFile()
    {
        if (SelectedFileNode == null) return;
        RemoveFileNode(SelectedFileNode);
    }

    [RelayCommand]
    private void RemoveAllFiles()
    {
        FileNodes.Clear();
        SelectedFileNode  = null;
        SelectedFileInfo  = "";
        RefreshCheckStates();
        StatusMessage = "All files removed.";
    }

    private void RemoveFileNode(MkvFileNode node)
    {
        node.PropertyChanged -= OnFileNodePropertyChanged;
        foreach (var t in node.Tracks)
            t.PropertyChanged -= OnTrackNodePropertyChanged;
        FileNodes.Remove(node);
        if (SelectedFileNode == node)
        {
            SelectedFileNode = FileNodes.FirstOrDefault();
        }
        RefreshCheckStates();
    }

    [RelayCommand]
    private void ToggleExpandCollapse()
    {
        if (!FileNodes.Any()) return;

        bool isAllExpanded = FileNodes.All(n => n.IsExpanded);
        bool targetState = !isAllExpanded;

        foreach (var node in FileNodes)
        {
            node.IsExpanded = targetState;
        }

        ExpandToggleText = targetState ? "▲ Collapse All" : "▼ Expand All";
    }

    // ─── Track Selection ──────────────────────────────────────────────────────

    [RelayCommand]
    private void SelectAllTracks()      => SetAllTracksChecked(true);

    [RelayCommand]
    private void DeselectAllTracks()    => SetAllTracksChecked(false);

    [RelayCommand]
    private void InvertSelection()
    {
        foreach (var file in FileNodes)
            foreach (var track in file.Tracks)
                track.IsChecked = !track.IsChecked;
    }

    [RelayCommand]
    private void SelectTracksByType(string type)
    {
        if (!Enum.TryParse<TrackNodeType>(type, out var nodeType)) return;
        foreach (var file in FileNodes)
            foreach (var track in file.Tracks.Where(t => t.NodeType == nodeType))
                track.IsChecked = true;
    }

    // ─── Bulk-select property setters (called from View CheckBox bindings) ────

    partial void OnAllTracksCheckedChanged(bool? value)
    {
        if (_refreshingCheckStates) return;
        if (value == true)  SetAllTracksChecked(true);
        if (value == false) SetAllTracksChecked(false);
    }

    partial void OnAllVideoCheckedChanged(bool? value)
    {
        if (_refreshingCheckStates) return;
        if (value is bool b) SetTypeChecked(TrackNodeType.Video, b);
    }

    partial void OnAllAudioCheckedChanged(bool? value)
    {
        if (_refreshingCheckStates) return;
        if (value is bool b) SetTypeChecked(TrackNodeType.Audio, b);
    }

    partial void OnAllSubtitlesCheckedChanged(bool? value)
    {
        if (_refreshingCheckStates) return;
        if (value is bool b) SetTypeChecked(TrackNodeType.Subtitle, b);
    }

    partial void OnAllChaptersCheckedChanged(bool? value)
    {
        if (_refreshingCheckStates) return;
        if (value is bool b) SetTypeChecked(TrackNodeType.Chapter, b);
    }

    partial void OnAllAttachmentsCheckedChanged(bool? value)
    {
        if (_refreshingCheckStates) return;
        if (value is bool b) SetTypeChecked(TrackNodeType.Attachment, b);
    }

    private void SetAllTracksChecked(bool value)
    {
        foreach (var file in FileNodes)
            foreach (var track in file.Tracks)
                track.IsChecked = value;
        RefreshCheckStates();
    }

    private void SetTypeChecked(TrackNodeType type, bool value)
    {
        foreach (var file in FileNodes)
            foreach (var track in file.Tracks.Where(t => t.NodeType == type))
                track.IsChecked = value;
        RefreshCheckStates();
    }

    private bool _refreshingCheckStates;

    private void RefreshCheckStates()
    {
        _refreshingCheckStates = true;
        _updatingFileCheck = true;
        foreach (var file in FileNodes)
        {
            if (!file.Tracks.Any()) file.IsChecked = false;
            else if (file.Tracks.All(t => t.IsChecked)) file.IsChecked = true;
            else if (file.Tracks.All(t => !t.IsChecked)) file.IsChecked = false;
            else file.IsChecked = null;
        }
        _updatingFileCheck = false;

        AllVideoChecked      = ComputeTypeState(TrackNodeType.Video);
        AllAudioChecked      = ComputeTypeState(TrackNodeType.Audio);
        AllSubtitlesChecked  = ComputeTypeState(TrackNodeType.Subtitle);
        AllChaptersChecked   = ComputeTypeState(TrackNodeType.Chapter);
        AllAttachmentsChecked = ComputeTypeState(TrackNodeType.Attachment);

        var all = AllTracks().ToList();
        if (all.Count == 0)       AllTracksChecked = false;
        else if (all.All(t => t.IsChecked))  AllTracksChecked = true;
        else if (all.All(t => !t.IsChecked)) AllTracksChecked = false;
        else                      AllTracksChecked = null;

        _refreshingCheckStates = false;
    }

    private bool? ComputeTypeState(TrackNodeType type)
    {
        var tracks = AllTracks().Where(t => t.NodeType == type).ToList();
        if (tracks.Count == 0)             return false;
        if (tracks.All(t => t.IsChecked))  return true;
        if (tracks.All(t => !t.IsChecked)) return false;
        return null; // mixed → indeterminate
    }

    private IEnumerable<TrackNode> AllTracks() =>
        FileNodes.SelectMany(f => f.Tracks);

    // ─── File info display ────────────────────────────────────────────────────

    partial void OnSelectedFileNodeChanged(MkvFileNode? value)
    {
        if (value?.SegmentInfo is { } info)
        {
            SelectedFileInfo =
                $"Duration:  {info.Duration}\n" +
                $"Muxing:    {info.MuxingApplication}\n" +
                $"Writing:   {info.WritingApplication}\n" +
                $"Date:      {info.Date}";
        }
        else
        {
            SelectedFileInfo = value?.FilePath ?? "";
        }
    }

    // ─── Output directory ─────────────────────────────────────────────────────

    [RelayCommand]
    private async Task BrowseOutputDirectory()
    {
        if (BrowseFolderFunc == null) return;
        var dir = await BrowseFolderFunc("Select Output Directory");
        if (dir != null)
        {
            OutputDirectory    = dir;
            UseSourceDirectory = false;
        }
    }

    partial void OnUseSourceDirectoryChanged(bool value)
    {
        if (value && SelectedFileNode != null)
            OutputDirectory = Path.GetDirectoryName(SelectedFileNode.FilePath) ?? "";
    }

    // ─── Extraction ───────────────────────────────────────────────────────────

    private bool ValidateForExtraction()
    {
        if (string.IsNullOrWhiteSpace(_settings.MkvToolNixPath) || !Directory.Exists(_settings.MkvToolNixPath))
        {
            StatusMessage = "Error: MKVToolNix path not configured. Open Settings to set it.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(OutputDirectory) && !UseSourceDirectory)
        {
            StatusMessage = "Error: Output directory not set.";
            return false;
        }
        if (!FileNodes.Any())
        {
            StatusMessage = "Error: No input files loaded.";
            return false;
        }
        if (!FileNodes.Any(f => f.Tracks.Any(t => t.IsChecked)))
        {
            StatusMessage = "Error: No tracks selected.";
            return false;
        }
        return true;
    }

    [RelayCommand]
    private async Task Extract()
    {
        if (!ValidateForExtraction()) return;

        IsExtracting = true;
        _cts          = new CancellationTokenSource();
        _extractor    = new gMKVExtract(_settings.MkvToolNixPath);

        _extractor.MkvExtractProgressUpdated += p =>
        {
            CurrentProgress = p;
        };
        _extractor.MkvExtractTrackUpdated += (_, name) =>
        {
            StatusMessage = $"Extracting: {name}";
        };

        try
        {
            var jobs      = BuildExtractionJobs();
            int completed = 0;

            foreach (var (file, segments) in jobs)
            {
                if (_cts.Token.IsCancellationRequested) break;

                var outputDir  = UseSourceDirectory ? Path.GetDirectoryName(file)! : OutputDirectory;
                var parameters = BuildParameters(file, segments, outputDir);

                await Task.Run(() => _extractor.ExtractMKVSegmentsThreaded(parameters), _cts.Token);

                if (_extractor.ThreadedException != null) throw _extractor.ThreadedException;

                completed++;
                TotalProgress = (int)((double)completed / jobs.Count * 100);
            }

            StatusMessage   = "Extraction completed successfully.";
            CurrentProgress = 100;

            if (PopupNotifications)
                WeakReferenceMessenger.Default.Send(new ShowNotificationMessage("Extraction Complete",
                    "All selected tracks have been extracted."));
        }
        catch (OperationCanceledException) { StatusMessage = "Extraction aborted."; }
        catch (Exception ex)               { StatusMessage = $"Extraction error: {ex.Message}"; }
        finally
        {
            IsExtracting = false;
            _extractor   = null;
        }
    }

    [RelayCommand]
    private void Abort()
    {
        _extractor?.Abort.ToString(); // ensure non-null
        if (_extractor != null) _extractor.Abort = true;
        StatusMessage = "Aborting…";
    }

    [RelayCommand]
    private void AbortAll()
    {
        if (_extractor != null) { _extractor.Abort = true; _extractor.AbortAll = true; }
        _cts?.Cancel();
        StatusMessage = "Aborting all…";
    }

    [RelayCommand]
    private void AddJobs()
    {
        var jobs = BuildExtractionJobs();
        if (!jobs.Any())
        {
            StatusMessage = "No tracks selected to add as jobs.";
            return;
        }

        foreach (var (file, segments) in jobs)
        {
            var outputDir = UseSourceDirectory ? Path.GetDirectoryName(file)! : OutputDirectory;
            Jobs.Add(new JobItem
            {
                SourceFile      = file,
                OutputDirectory = outputDir,
                Tracks          = string.Join(", ", segments.Take(3).Select(s => s.ToString()))
                                  + (segments.Count > 3 ? $" +{segments.Count - 3} more" : ""),
                Status          = "Ready",
                Parameters      = BuildParameters(file, segments, outputDir)
            });
        }
        StatusMessage = $"Added {jobs.Count} job(s) to queue.";
    }

    // ─── Navigation / Window commands ────────────────────────────────────────

    [RelayCommand] private void OpenLogs()       => WeakReferenceMessenger.Default.Send(new OpenLogsMessage(LogViewModel));
    [RelayCommand] private void OpenJobManager() => WeakReferenceMessenger.Default.Send(new OpenJobManagerMessage(this));
    [RelayCommand] private void OpenSettings()   => WeakReferenceMessenger.Default.Send(new OpenSettingsMessage(_settings, this));
    [RelayCommand] private void OpenAbout()      => WeakReferenceMessenger.Default.Send(new OpenAboutMessage());

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private List<(string file, List<gMKVSegment> segments)> BuildExtractionJobs() =>
        FileNodes
            .Select(f => (f.FilePath,
                          f.Tracks.Where(t => t.IsChecked && t.Segment != null)
                                  .Select(t => t.Segment!)
                                  .ToList()))
            .Where(x => x.Item2.Count > 0)
            .ToList();

    private gMKVExtractSegmentsParameters BuildParameters(string file, List<gMKVSegment> segments, string outputDir)
    {
        // Determine extra extraction mode overrides
        var timecodesMode = SelectedExtractionMode switch
        {
            "Timecodes_v2" or "Timestamps_v2" => TimecodesExtractionMode.OnlyTimecodes,
            _                                 => TimecodesExtractionMode.NoTimecodes
        };
        var cueMode = SelectedExtractionMode switch
        {
            "Cues" => CuesExtractionMode.OnlyCues,
            _      => CuesExtractionMode.NoCues
        };

        return new gMKVExtractSegmentsParameters
        {
            MKVFile                = file,
            MKVSegmentsToExtract   = segments,
            OutputDirectory        = outputDir,
            ChapterType            = Enum.TryParse<MkvChapterTypes>(SelectedChapterType, out var ct) ? ct : MkvChapterTypes.XML,
            TimecodesExtractionMode = timecodesMode,
            CueExtractionMode      = cueMode,
            FilenamePatterns       = _settings.ToFilenamePatterns(),
            DisableBomForTextFiles = _settings.DisableBomForTextFiles,
            OverwriteExistingFile  = OverwriteExistingFiles,
            UseRawExtractionMode   = _settings.UseRawExtraction,
            UseFullRawExtractionMode = _settings.UseFullRawExtraction
        };
    }

    public void SaveSettings()
    {
        _settings.LastOutputDirectory = OutputDirectory;
        _settings.UseSourceDirectory  = UseSourceDirectory;
        _settings.AppendOnDragDrop    = AppendOnDragDrop;
        _settings.OverwriteExistingFiles = OverwriteExistingFiles;
        _settings.PopupNotifications  = PopupNotifications;
        SettingsService.Save(_settings);
    }
}

// ─── Message types ────────────────────────────────────────────────────────────

public record OpenLogsMessage(LogWindowViewModel LogViewModel);
public record OpenJobManagerMessage(MainWindowViewModel MainVm);
public record OpenSettingsMessage(xMKVExtractGUI.Services.AppSettings Settings, MainWindowViewModel MainVm);
public record OpenAboutMessage();
public record ShowNotificationMessage(string Title, string Body);
public record SaveLogMessage(string LogContent);

/// <summary>Minimal synchronous weak-reference message bus.</summary>
public sealed class WeakReferenceMessenger
{
    public static WeakReferenceMessenger Default { get; } = new();
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public void Register<TMessage>(object _, Action<TMessage> handler)
    {
        var type = typeof(TMessage);
        if (!_handlers.TryGetValue(type, out var list)) { list = []; _handlers[type] = list; }
        list.Add(handler);
    }

    public void Send<TMessage>(TMessage message)
    {
        if (!_handlers.TryGetValue(typeof(TMessage), out var list)) return;
        foreach (var d in list.ToList())
            if (d is Action<TMessage> a) a(message);
    }
}
