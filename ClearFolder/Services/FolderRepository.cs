using ClearFolder.Models;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClearFolder.Services;

public class FolderRepository(IDialogService dialogService) : IFolderRepository
{
    const string KeyName = "Folders";
    const string SubKeyName = @"SOFTWARE\ClearFolders";

    public void AddFolderToRegistry(ObservableCollection<Folder> foldersList)
    {
        RegistryKey key = Registry.CurrentUser.CreateSubKey(SubKeyName);
        if (key.GetValue(KeyName) != null) key.DeleteValue(KeyName);
        if (!foldersList.Any()) return;
        string folders = string.Join(",", foldersList.Where(f => !string.IsNullOrEmpty(f.Name)).Select(f => f.Name).ToArray());
        key.SetValue(KeyName, folders);
    }

    public ObservableCollection<Folder> GetFolders()
    {
        List<Folder> folders = [];
        RegistryKey key = Registry.CurrentUser.CreateSubKey(SubKeyName);
        string keyValue = $"{key.GetValue(KeyName)}";
        if (string.IsNullOrWhiteSpace(keyValue)) return null!;
        folders.AddRange($"{keyValue}".Split(",")
            .Select(folder => (Folder)new() { Name = folder }));
        folders.RemoveAll(f => !Directory.Exists(f.Name));
        return new ObservableCollection<Folder>(folders);
    }

    public async Task OpenFolder(Folder? folder, string dialogName)
    {
        string? path = folder?.Name;
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"" });
        }
        else
        {
            await dialogService.ShowMessage(dialogName, $"{path}\nDirectory does not exist!");
        }
    }
}