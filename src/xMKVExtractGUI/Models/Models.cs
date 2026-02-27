using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using gMKVToolNix.Segments;
using gMKVToolNix.MkvExtract;

namespace xMKVExtractGUI.Models;

/// <summary>Represents a loaded MKV file node in the track tree.</summary>
public partial class MkvFileNode : ObservableObject
{
    [ObservableProperty] private string _filePath = "";
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private bool? _isChecked = false;
    [ObservableProperty] private gMKVSegmentInfo? _segmentInfo;

    public string FileName => System.IO.Path.GetFileName(FilePath);
    public ObservableCollection<TrackNode> Tracks { get; } = [];
}

/// <summary>Represents a single track/segment row inside a file node.</summary>
public partial class TrackNode : ObservableObject
{
    [ObservableProperty] private bool _isChecked;
    [ObservableProperty] private string _displayText = "";
    [ObservableProperty] private TrackNodeType _nodeType;

    public gMKVSegment? Segment { get; set; }

    public string TypeIcon => NodeType switch
    {
        TrackNodeType.Video      => "IconVideo",
        TrackNodeType.Audio      => "IconAudio",
        TrackNodeType.Subtitle   => "IconSubtitle",
        TrackNodeType.Chapter    => "IconChapters",
        TrackNodeType.Attachment => "IconAttachment",
        _                        => "IconFile"
    };
}

public enum TrackNodeType
{
    Video,
    Audio,
    Subtitle,
    Chapter,
    Attachment,
    Unknown
}

/// <summary>A queued extraction job shown in the Job Manager.</summary>
public partial class JobItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;
    [ObservableProperty] private string _status = "Ready";
    [ObservableProperty] private string _sourceFile = "";
    [ObservableProperty] private string _outputDirectory = "";
    [ObservableProperty] private string _tracks = "";
    [ObservableProperty] private int _progress;

    public DateTime CreatedAt { get; } = DateTime.Now;
    public gMKVExtractSegmentsParameters? Parameters { get; set; }
}

public class IconConverter : IValueConverter
{
    public static IconConverter Instance { get; } = new IconConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && Application.Current != null)
        {
            if (Application.Current.TryGetResource(key, out var resource))
                return resource;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}