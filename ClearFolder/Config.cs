using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClearFolder.Models;
using Microsoft.Win32;

namespace ClearFolder
{
    public static class Config
    {
        const string KeyName = "Folders";
        public static void AddFolderToRegistry(List<Folder> foldersList)
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\ClearFolders");
            if (key == null) return;
            if (key.GetValue(KeyName) != null) key.DeleteValue(KeyName);
            if (!foldersList.Any()) return;
            string folders = string.Join(",", foldersList.Where(f => !string.IsNullOrEmpty(f.Name)).Select(f => f.Name).ToArray());
            key.SetValue(KeyName, folders);
        }

        public static List<Folder> GetFolders()
        {
            List<Folder> folders = new();
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\ClearFolders");
            if (key == null) return null;
            string keyValue = $"{key.GetValue(KeyName)}";
            if (string.IsNullOrWhiteSpace(keyValue)) return null;
            folders.AddRange($"{keyValue}".Split(",")
                .Select(folder => (Folder)new() { Name = folder }));
            folders.RemoveAll(f => !Directory.Exists(f.Name));
            return folders;
        }
    }
}
