using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using ClearFolder.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.VisualBasic.FileIO;
using PropertyChanged;

namespace ClearFolder
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        public List<Folder> Folders { get; set; }
        public ICollectionView FoldersCollection { get; set; }
        public Folder SelectedFolder { get; set; }
        public int DaysBack { get; set; }
        public RelayCommand AddFolderCommand => new(AddFolderCommand_Execute);
        public RelayCommand DeleteFolderCommand => new(DeleteFolderCommand_Execute);
        public RelayCommand IncreaseDaysCommand => new(DaysCommand_Execute);
        public RelayCommand DecreaseDaysCommand => new(DaysCommand_Execute);
        public RelayCommand<bool> ChangeDeleteType => new(ChangeDeleteType_OnClick);
        public AsyncRelayCommand ClearFolderCommand => new(ClearFolderCommand_Execute);
        public bool IsAddDays { get; set; }
        public int DaysChangeValue { get; set; }
        public bool IsDeletePermanently { get; set; }
        public RelayCommand OpenSelectedFolder => new(OpenSelectedFolder_Execute);

        public MainWindowViewModel()
        {
            Folders = Config.GetFolders();
            FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
            SelectedFolder = Folders.FirstOrDefault();
            DaysBack = 7;
        }

        void DaysCommand_Execute()
        {
            if (IsAddDays)
            {
                DaysBack += DaysChangeValue;
            }
            else if (DaysBack - DaysChangeValue > 0)
            {
                DaysBack -= DaysChangeValue;
            }
            else
            {
                DaysBack = 0;
            }
        }

        public void IncreaseDecreaseDays(bool isAdd, int value)
        {
            IsAddDays = isAdd;
            DaysChangeValue = value;
        }

        void AddFolderCommand_Execute()
        {
            using FolderBrowserDialog fbd = new();
            DialogResult result = fbd.ShowDialog();
            if (result != DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath)) return;
            string selectedFolder = fbd.SelectedPath;
            if (Folders.Any(f => f.Name == selectedFolder)) return;
            Folders.Add(new() { Name = selectedFolder });
            if (SelectedFolder == null)
            {
                SelectedFolder = new() { Name = selectedFolder };
            }
            else
            {
                SelectedFolder.Name = selectedFolder;
            }
            FoldersCollection?.Refresh();
            Config.AddFolderToRegistry(Folders);
        }

        void DeleteFolderCommand_Execute()
        {
            if (SelectedFolder == null || !Folders.Contains(SelectedFolder)) return;
            if (MessageBox.Show($"Remove {SelectedFolder.Name} from list?", "Confirm delete", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) == DialogResult.Cancel) return;
            Folders.Remove(SelectedFolder);
            Config.AddFolderToRegistry(Folders);
            FoldersCollection?.Refresh();
        }

        async Task ClearFolderCommand_Execute()
        {
            if (!Folders.Any()) return;
            foreach (Folder folder in Folders.Where(f => f.IsSelected))
            {
                await ClearFolder(folder.Name);
            }
        }

        async Task ClearFolder(string folderPath)
        {
            List<FileInfo> deleteFiles = Directory.GetFiles(folderPath).Select(f => new FileInfo(f))
                .Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * DaysBack)).ToList();
            if (!deleteFiles.Any())
            {
                MessageBox.Show($"Files older than {DaysBack} day{(DaysBack > 1 ? "s" : "")} not found in\n{folderPath}",
                    "No Files", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Delete {deleteFiles.Count} files from {folderPath}?", "Confirm Delete",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel) return;
            RecycleOption recycleOption = IsDeletePermanently
                ? RecycleOption.DeletePermanently
                : RecycleOption.SendToRecycleBin;
            _ = DialogHost.Show(new ProgressBar());
            await Task.Run(() => deleteFiles
                .ForEach(f => FileSystem.DeleteFile(f.FullName, UIOption.OnlyErrorDialogs, recycleOption)));
            DialogHost.Close("RootDialog");
        }

        void ChangeDeleteType_OnClick(bool isDelete)
        {
            IsDeletePermanently = isDelete;
        }

        void OpenSelectedFolder_Execute()
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
                MessageBox.Show($"{folderPath}\nDirectory does not exist!",
                    "Open Folder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
