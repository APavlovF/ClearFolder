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
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.VisualBasic.FileIO;
using PropertyChanged;
using Shell32;
using Folder = ClearFolder.Models.Folder;
using SearchOption = System.IO.SearchOption;

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
        public RelayCommand<int> IncreaseDaysCommand => new(DaysCommand_Execute);
        public RelayCommand<int> DecreaseDaysCommand => new(DaysCommand_Execute);
        public RelayCommand<bool> ChangeDeleteType => new(ChangeDeleteType_OnClick);
        public AsyncRelayCommand ClearFolderCommand => new(ClearFolderCommand_Execute);
        public AsyncRelayCommand EmptyRecycleBin => new(EmptyRecycleBin_Execute);
        public bool IsAddDays { get; set; }
        public bool IsDeletePermanently { get; set; }
        public RelayCommand OpenSelectedFolder => new(OpenSelectedFolder_Execute);
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
                Folders = new() { new() { Name = selectedFolder, DaysBack = DaysBack } };
                FoldersCollection = CollectionViewSource.GetDefaultView(Folders);
            }
            if (Folders.Any(f => f.Name == selectedFolder)) return;
            Folders.Add(new() { Name = selectedFolder, DaysBack = DaysBack });
            SelectedFolder ??= new() { Name = selectedFolder };
            FoldersCollection?.Refresh();
            Config.AddFolderToRegistry(Folders);
            SetClearButtonEnabled();
        }

        void DeleteFolderCommand_Execute()
        {
            if (SelectedFolder == null || !Folders.Contains(SelectedFolder)) return;
            if (MessageBox.Show($"Remove {SelectedFolder.Name} from list?", "Confirm delete", MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) == DialogResult.Cancel) return;
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
            DeleteEmptySubdirectories(folderPath);

            DialogHost.Close("RootDialog");
            Folders.First(f => f.Name == folderPath).DeleteFilesCount = failDelete;
            GetRecycleData();

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

            static void DeleteEmptySubdirectories(string parentDirectory)
            {
                Parallel.ForEach(Directory.GetDirectories(parentDirectory), directory =>
                {
                    DeleteEmptySubdirectories(directory);
                    if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory, false);
                    }
                });
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


        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        enum RecycleFlags : uint
        {
            SHRB_NOCONFIRMATION = 0x00000001
        }

        async Task EmptyRecycleBin_Execute()
        {
            DialogResult result = MessageBox.Show("Are you sure you want to delete all the items from recycle bin?",
                "Clear recycle bin", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return;
            //_ = DialogHost.Show(new ProgressBar());
            await Task.Run(DeleteFiles);
            //DialogHost.Close("RootDialog");

            void DeleteFiles()
            {
                try
                {
                    _ = SHEmptyRecycleBin(IntPtr.Zero, null, RecycleFlags.SHRB_NOCONFIRMATION);
                    MessageBox.Show("Files deleted.", "Clear recycle bin",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Files couldn't be recycled." + ex.Message, "Clear recycle bin", MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                }
                RecycleCount = "0 Items";
                RecycleSize = "0 B";
            }
        }

        public void GetRecycleData()
        {
            Shell shell = new Shell();
            Shell32.Folder recycleBin = shell.NameSpace(10);
            int count = 0;
            int size = 0;
            foreach (FolderItem2 file in recycleBin.Items())
            {
                count++;
                size += file.Size;
            }

            RecycleCount = $"{count} Items";
            RecycleSize = Folder.FormatFileSize(size);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
