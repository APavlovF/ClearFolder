using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        public Folder SelectedFolder { get; set; } = new();
        public int DaysBack { get; set; }
        public RelayCommand AddFolderCommand => new(AddFolderCommand_Execute);
        public RelayCommand DeleteFolderCommand => new(DeleteFolderCommand_Execute);
        public RelayCommand IncreaseDaysCommand => new(DaysCommand_Execute);
        public RelayCommand DecreaseDaysCommand => new(DaysCommand_Execute);
        public AsyncRelayCommand ClearFolderCommand => new(ClearFolderCommand_Execute);
        public bool IsAddDays { get; set; }
        public int DaysChangeValue { get; set; }
        public MainWindowViewModel()
        {
            Folders = Config.GetFolders();
            FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
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
            if (string.IsNullOrEmpty(SelectedFolder.Name)) return;
            if (MessageBox.Show($"Clear files from {SelectedFolder.Name}?", "Confirm clear", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) == DialogResult.Cancel) return;
            _ = DialogHost.Show(new ProgressBar());
            await Task.Run(() => Directory.GetFiles(SelectedFolder.Name)
                .Select(f => new FileInfo(f)).Where(f => f.CreationTime < DateTime.Now.AddDays(-1 * DaysBack)).ToList()
                .ForEach(f => FileSystem.DeleteFile(f.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin)));
            DialogHost.Close("RootDialog");
        }
    }
}
