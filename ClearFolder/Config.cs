using System.Collections.Generic;
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
            if(key.GetValue(KeyName) != null) key.DeleteValue(KeyName);
            if (!foldersList.Any()) return;
            string folders = string.Join(",", foldersList.Where(f => !string.IsNullOrEmpty(f.Name)).Select(f => f.Name).ToArray());
            key.SetValue(KeyName, folders);
        }

        public static List<Folder> GetFolders()
        {
            List<Folder> folders = new();
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\ClearFolders");
            if (key == null) return folders;
            folders.AddRange($"{key.GetValue(KeyName)}".Split(",")
                .Select(folder => (Folder)new() { Name = folder }));
            return folders;
        }
    }
}
