using System;
using System.IO;
using System.Text.Json;
using gMKVToolNix.MkvExtract;

namespace xMKVExtractGUI.Services;

public class AppSettings
{
    public string MkvToolNixPath                 { get; set; } = "";
    public string LastOutputDirectory            { get; set; } = "";
    public bool   UseSourceDirectory             { get; set; } = false;
    public bool   AppendOnDragDrop               { get; set; } = false;
    public bool   OverwriteExistingFiles         { get; set; } = false;
    public bool   PopupNotifications             { get; set; } = true;
    public bool   DisableBomForTextFiles         { get; set; } = false;
    public bool   UseRawExtraction               { get; set; } = false;
    public bool   UseFullRawExtraction           { get; set; } = false;
    public string VideoTrackFilenamePattern      { get; set; } = "{FilenameNoExt}_track{TrackNumber:00}_{Language}";
    public string AudioTrackFilenamePattern      { get; set; } = "{FilenameNoExt}_track{TrackNumber:00}_{Language}";
    public string SubtitleTrackFilenamePattern   { get; set; } = "{FilenameNoExt}_track{TrackNumber:00}_{Language}";
    public string ChapterFilenamePattern         { get; set; } = "{FilenameNoExt}_chapters";
    public string AttachmentFilenamePattern      { get; set; } = "{AttachmentFilename}";
    public string TagsFilenamePattern            { get; set; } = "{FilenameNoExt}_tags";
}

public static class SettingsService
{
    // store settings in the same directory as the executable
    private static readonly string SettingsPath = Path.Combine(
    AppContext.BaseDirectory,
    "settings.json");

//    private static readonly string SettingsPath = Path.Combine(
//        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//        "xMKVExtractGUI",
//        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* return defaults */ }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* silently fail */ }
    }

    public static gMKVExtractFilenamePatterns ToFilenamePatterns(this AppSettings s) =>
        new gMKVExtractFilenamePatterns
        {
            VideoTrackFilenamePattern    = s.VideoTrackFilenamePattern,
            AudioTrackFilenamePattern    = s.AudioTrackFilenamePattern,
            SubtitleTrackFilenamePattern = s.SubtitleTrackFilenamePattern,
            ChapterFilenamePattern       = s.ChapterFilenamePattern,
            AttachmentFilenamePattern    = s.AttachmentFilenamePattern,
            TagsFilenamePattern          = s.TagsFilenamePattern
        };
}
