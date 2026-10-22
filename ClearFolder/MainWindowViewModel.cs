using ClearFolder.Dialogs;
using ClearFolder.Models;
using ClearFolder.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Folder = ClearFolder.Models.Folder;

namespace ClearFolder;

public partial class MainWindowViewModel : ObservableObject
{
    readonly IDialogService _dialogService;
    readonly IFolderCleaner _folderCleaner;
    readonly IFolderRepository _folderRepository;
    readonly IRecycleBinService _recycleBinService;
    public string DialogName { get; } = $"Dialog-{DateTime.Now.GetHashCode()}";
    public ObservableCollection<Folder> Folders { get; }
    [ObservableProperty] string _progressStatus = "";   // -> ProgressStatus
    [ObservableProperty] Folder? _selectedFolder;       // -> SelectedFolder
    [ObservableProperty] int _daysBack = 7;             // -> DaysBack
    [ObservableProperty] bool _isDeletePermanently;     // -> IsDeletePermanently
    [ObservableProperty] bool _clearButtonEnabled;      // -> ClearButtonEnabled
    [ObservableProperty] string _deleteFilesInfo = "";  // -> DeleteFilesInfo
    [ObservableProperty] string _recycleSize = "0 B";   // -> RecycleSize

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecycleEnabled))]
    string _recycleCount = RecycleBinService.ZeroItems; // -> RecycleCount

    public bool RecycleEnabled => RecycleCount != RecycleBinService.ZeroItems;

    public MainWindowViewModel(IDialogService dialogService,
        IFolderCleaner folderCleaner,
        IFolderRepository folderRepository,
        IRecycleBinService recycleBinService)
    {
        _dialogService = dialogService;
        _folderCleaner = folderCleaner;
        _folderRepository = folderRepository;
        _recycleBinService = recycleBinService;
        DaysBack = 7;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
        Folders = _folderRepository.GetFolders() ?? [];
        SelectedFolder = Folders.FirstOrDefault();
        foreach (Folder f in Folders)
        {
            f.DaysBack = DaysBack;
            Subscribe(f);
        }
        SetClearButtonEnabled();
        GetRecycleData();
    }

    public void SelectedChanged()
    {
        List<Folder> selected = Folders.Where(f => f.IsSelected).ToList();
        int count = selected.Sum(f => f.DeleteFilesCount);
        long size = selected.Sum(f => f.DeleteFilesBytes);

        DeleteFilesInfo = count == 0 ? string.Empty : $"Delete {count} Files - {Folder.FormatFileSize(size)}";
        SetClearButtonEnabled();
    }

    public void SetClearButtonEnabled() => ClearButtonEnabled = Folders.Any(f => f is { IsSelected: true, DeleteFilesCount: > 0 });

    [RelayCommand]
    void IncreaseDays(int delta)
    {
        DaysBack += delta;
        UpdateFolders();
    }

    [RelayCommand]
    void DecreaseDays(int delta)
    {
        DaysBack = Math.Max(0, DaysBack - delta);
        UpdateFolders();
    }

    void Subscribe(Folder folder) => folder.PropertyChanged += Folder_PropertyChanged;

    void Unsubscribe(Folder folder) => folder.PropertyChanged -= Folder_PropertyChanged;

    void Folder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Folder.IsSelected))
        {
            SelectedChanged();
        }
    }

    void UpdateFolders()
    {
        foreach (Folder f in Folders)
        {
            f.DaysBack = DaysBack;
            Subscribe(f);
        }

        SelectedChanged();
    }

    [RelayCommand]
    void AddFolder()
    {
        string path = _dialogService.GetFolder();
        if (string.IsNullOrWhiteSpace(path) || Folders.Any(f => f.Name == path)) return;

        Folder folder = new Folder { Name = path, DaysBack = DaysBack };
        Subscribe(folder);
        Folders.Add(folder);
        SelectedFolder = folder;
        _folderRepository.AddFolderToRegistry(Folders);
        SetClearButtonEnabled();
    }

    [RelayCommand]
    async Task DeleteFolderAsync()
    {
        if (SelectedFolder == null || !Folders.Contains(SelectedFolder)) return;
        if (!await _dialogService.Confirm($"Remove {SelectedFolder.Name} from list?", DialogName, DialogIcon.Warning)) return;

        Folders.Remove(SelectedFolder);
        Unsubscribe(SelectedFolder);
        _folderRepository.AddFolderToRegistry(Folders);
        SelectedChanged();
    }

    [RelayCommand]
    async Task ClearFolderAsync()
    {
        foreach (Folder folder in Folders.Where(f => f.IsSelected))
        {
            await _folderCleaner.ClearFolder(folder.Name, DaysBack, DialogName,
                new Progress<string>(s => ProgressStatus = s),
                IsDeletePermanently ? RecycleOption.DeletePermanently : RecycleOption.SendToRecycleBin);
            Folders.First(f => f.Name == folder.Name).Refresh();
            GetRecycleData();
        }

        SelectedChanged();
    }

    [RelayCommand]
    void ChangeDeleteType(bool isDelete) => IsDeletePermanently = isDelete;

    [RelayCommand]
    async Task OpenSelectedFolderAsync(Folder folder) => await _folderRepository.OpenFolder(folder, DialogName);

    [RelayCommand]
    async Task EmptyRecycleBinAsync()
    {
        if (!await _dialogService.Confirm("Are you sure you want to delete all the items from recycle bin?", DialogName, DialogIcon.Warning))
            return;

        (bool success, string count, string size) result = await Task.Run(_recycleBinService.TryEmptyRecycleBin);
        RecycleCount = result.count;
        RecycleSize = result.size;
        await OpenDialog.ShowMessage(DialogName, result.success ? "Files deleted." : "Files couldn't be recycled.");
        System.Windows.Application.Current?.MainWindow?.Activate();
    }

    void GetRecycleData()
    {
        (string RecycleCount, string RecycleSize) binData = _recycleBinService.GetRecycleData();
        RecycleCount = binData.RecycleCount;
        RecycleSize = binData.RecycleSize;
    }
}