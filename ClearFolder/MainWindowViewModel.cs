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
            DaysBack = 7;
            Folders = Config.GetFolders();
            if (Folders == null) return;
            FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
            SelectedFolder = Folders.FirstOrDefault();
            Folders.ForEach(f => f.DaysBack = DaysBack);
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
            Folders.ForEach(f => f.DaysBack = DaysBack);
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
            if (Folders == null)
            {
                Folders = new() { new() { Name = selectedFolder, DaysBack = DaysBack } };
                FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
            }
            if (Folders.Any(f => f.Name == selectedFolder)) return;
            Folders.Add(new() { Name = selectedFolder, DaysBack = DaysBack });
            SelectedFolder ??= new() { Name = selectedFolder };
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
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)
                            && f.CreationTime < DateTime.Now.AddDays(-1 * DaysBack)).ToList();
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

            int failDelete = 0;
            await Task.Run(DeleteFiles);

            DialogHost.Close("RootDialog");
            Folders.First(f => f.Name == folderPath).ToolTipText = $"Delete {failDelete} files";

            void DeleteFiles()
            {
                foreach (FileInfo file in deleteFiles)
                {
                    try
                    {
                        FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, recycleOption);
                    }
                    catch (Exception)
                    {
                        failDelete++;
                    }
                }
            }
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
