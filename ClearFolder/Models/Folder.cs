using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClearFolder.Models;

public partial class Folder : ObservableObject
{
    [ObservableProperty] bool _isSelected;
    [ObservableProperty] int _daysBack;
    [ObservableProperty] long _deleteFilesBytes;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectEnabled))]
    int _deleteFilesCount;
    [ObservableProperty] string _deleteFilesSize = "0 B";
    [ObservableProperty] string _name = "";

    public bool SelectEnabled => DeleteFilesCount != 0;

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(Name) || !Directory.Exists(Name)) return;

        EnumerationOptions options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true
        };

        DateTime cutoff = DateTime.Now.AddDays(-DaysBack);
        List<FileInfo> files = Directory.GetFiles(Name, "*", options)
            .Select(f => new FileInfo(f))
            .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden) && f.CreationTime < cutoff)
            .ToList();

        long size = files.Sum(f => f.Length);
        DeleteFilesBytes = size;
        DeleteFilesSize = size.FormatFileSize();
        DeleteFilesCount = files.Count;

        if (DeleteFilesCount == 0) IsSelected = false;
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDaysBackChanged(int value) => Refresh();
}