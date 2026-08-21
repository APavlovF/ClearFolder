using ClearFolder.Dialogs;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualBasic.FileIO;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using Folder = ClearFolder.Models.Folder;
using SearchOption = System.IO.SearchOption;

namespace ClearFolder;

[AddINotifyPropertyChangedInterface]
public class MainWindowViewModel
{
    readonly RecycleBinHelper _recycleBinHelper = new();

    public string ProgressStatus { get; set; }
    public List<Folder> Folders { get; set; }
    public ICollectionView FoldersCollection { get; set; }
    public Folder SelectedFolder { get; set; }
    public int DaysBack { get; set; }
    public RelayCommand AddFolderCommand => new(AddFolderCommand_Execute);
    public AsyncRelayCommand DeleteFolderCommand => new(DeleteFolderCommand_Execute);
    public string DialogName { get; } = $"Dialog-{DateTime.Now.GetHashCode()}";
    public RelayCommand<int> IncreaseDaysCommand => new(DaysCommand_Execute);
    public RelayCommand<int> DecreaseDaysCommand => new(DaysCommand_Execute);
    public RelayCommand<bool> ChangeDeleteType => new(ChangeDeleteType_OnClick);
    public AsyncRelayCommand ClearFolderCommand => new(ClearFolderCommand_Execute);
    public AsyncRelayCommand EmptyRecycleBin => new(EmptyRecycleBin_Execute);
    public bool IsAddDays { get; set; }
    public bool IsDeletePermanently { get; set; }
    public AsyncRelayCommand OpenSelectedFolder => new(OpenSelectedFolder_Execute);
    public bool ClearButtonEnabled { get; set; }
    public string RecycleCount { get; set; }
    public string RecycleSize { get; set; }
    public bool RecycleEnabled => RecycleCount != "0 Items";
    public string DeleteFilesInfo { get; set; }

    public MainWindowViewModel()
    {
        DaysBack = 7;
        Folders = Config.GetFolders();
        if (Folders == null) return;
        FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
        SelectedFolder = Folders.FirstOrDefault();
        Folders.ForEach(f => f.DaysBack = DaysBack);
        SetClearButtonEnabled();
        GetRecycleData();
    }

    public void SelectedChanged()
    {
        int count = Folders.Where(f => f.IsSelected).Sum(f => f.DeleteFilesCount);
        long size = Folders.Where(f => f.IsSelected).Sum(f => f.DeleteFilesBytes);
        DeleteFilesInfo = count == 0 ? "" : $"Delete {count} Files - {Folder.FormatFileSize(size)}";
        SetClearButtonEnabled();
    }

    public void SetClearButtonEnabled()
    {
        ClearButtonEnabled = Folders.Any(f => f.IsSelected && f.DeleteFilesCount > 0);
    }

    void DaysCommand_Execute(int daysChangeValue)
    {
        if (IsAddDays)
        {
            DaysBack += daysChangeValue;
        }
        else if (DaysBack - daysChangeValue > 0)
        {
            DaysBack -= daysChangeValue;
        }
        else
        {
            DaysBack = 0;
        }
        Folders.ForEach(f => f.DaysBack = DaysBack);
        SelectedChanged();
    }

    void AddFolderCommand_Execute()
    {
        using FolderBrowserDialog fbd = new();
        DialogResult result = fbd.ShowDialog();
        if (result != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath)) return;
        string selectedFolder = fbd.SelectedPath;
        if (Folders == null)
        {
            Folders = [new() { Name = selectedFolder, DaysBack = DaysBack }];
            FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
        }
        if (Folders.Any(f => f.Name == selectedFolder)) return;
        Folders.Add(new() { Name = selectedFolder, DaysBack = DaysBack });
        SelectedFolder ??= new() { Name = selectedFolder };
        FoldersCollection?.Refresh();
        Config.AddFolderToRegistry(Folders);
        SetClearButtonEnabled();
    }

    async Task DeleteFolderCommand_Execute()
    {
        if (SelectedFolder == null || !Folders.Contains(SelectedFolder)) return;
        if (await OpenYesNoCancelDialog($"Remove {SelectedFolder.Name} from list?", true) != DialogResult.OK) return;
        Folders.Remove(SelectedFolder);
        Config.AddFolderToRegistry(Folders);
        FoldersCollection?.Refresh();
        SelectedChanged();
    }

    async Task ClearFolderCommand_Execute()
    {
        if (!Folders.Any()) return;
        foreach (Folder folder in Folders.Where(f => f.IsSelected))
        {
            await ClearFolder(folder.Name);
        }
        SelectedChanged();
    }

    async Task ClearFolder(string folderPath)
    {
        List<FileInfo> deleteFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f)).Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)
                                                     && f.CreationTime < DateTime.Now.AddDays(-1 * DaysBack)).ToList();
        if (!deleteFiles.Any())
        {
            await OpenDialog.ShowMessage(DialogName,
                $"Files older than {DaysBack} day{(DaysBack > 1 ? "s" : "")} not found in\n{folderPath}");
            return;
        }

        if (await OpenYesNoCancelDialog($"Delete {deleteFiles.Count} files from {folderPath}?", true) !=
            DialogResult.OK) return;
        RecycleOption recycleOption = IsDeletePermanently
            ? RecycleOption.DeletePermanently
            : RecycleOption.SendToRecycleBin;
        _ = DialogHost.Show(new ProgressBar());

        int failDelete = 0;
        await Task.Run(DeleteFiles);
        DeleteEmptySubdirectories(folderPath);

        DialogHost.Close(DialogName);
        Folders.First(f => f.Name == folderPath).DeleteFilesCount = failDelete;
        GetRecycleData();

        void DeleteFiles()
        {
            foreach (FileInfo file in deleteFiles)
            {
                try
                {
                    ProgressStatus = $"Delete file: {file.Name}";
                    FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, recycleOption);
                }
                catch (Exception)
                {
                    failDelete++;
                }
            }
        }

        void DeleteEmptySubdirectories(string parentDirectory)
        {
            Parallel.ForEach(Directory.GetDirectories(parentDirectory), directory =>
            {
                DeleteEmptySubdirectories(directory);
                if (Directory.EnumerateFileSystemEntries(directory).Any()) return;
                ProgressStatus = $"Delete folder: {directory}";
                Directory.Delete(directory, false);
            });
        }
    }

    void ChangeDeleteType_OnClick(bool isDelete)
    {
        IsDeletePermanently = isDelete;
    }

    async Task<DialogResult> OpenYesNoCancelDialog(string message, bool useOkCancel)
    {
        object ob = await OpenDialog.ShowYesNoCancel(DialogName, message, useOkCancel);
        return (DialogResult)ob;
    }

    async Task OpenSelectedFolder_Execute()
    {
        if (SelectedFolder == null) return;
        string folderPath = SelectedFolder.Name;
        if (Directory.Exists(folderPath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = folderPath,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
        else
        {
            await OpenDialog.ShowMessage(DialogName, $"{folderPath}\nDirectory does not exist!");
        }
    }


    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

    enum RecycleFlags : uint
    {
        SHRB_NOCONFIRMATION = 0x00000001
    }

    async Task EmptyRecycleBin_Execute()
    {
        DialogResult result =
            await OpenYesNoCancelDialog("Are you sure you want to delete all the items from recycle bin?", true);
        if (result is DialogResult.Cancel or DialogResult.No) return;
        if (await Task.Run(DeleteFiles))
        {
            await OpenDialog.ShowMessage(DialogName, "Files deleted.");
        }
        else
        {
            await OpenDialog.ShowMessage(DialogName, "Files couldn't be recycled.");
        }

        System.Windows.Application.Current?.MainWindow?.Activate();

        bool DeleteFiles()
        {
            try
            {
                _ = SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHRB_NOCONFIRMATION);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
            RecycleCount = "0 Items";
            RecycleSize = "0 B";

            return true;
        }
    }

    public void GetRecycleData()
    {
        (string RecycleCount, string RecycleSize) binData = _recycleBinHelper.GetRecycleData();
        RecycleCount = binData.RecycleCount;
        RecycleSize = binData.RecycleSize;
    }
}