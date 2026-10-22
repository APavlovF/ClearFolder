using ClearFolder.Dialogs;
using ClearFolder.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SearchOption = System.IO.SearchOption;

namespace ClearFolder.Services;

public class FolderCleaner : IFolderCleaner
{
    public async Task ClearFolder(string folderPath, int daysBack, string dialogName, IProgress<string> progress,
        RecycleOption recycleOption)
    {
        List<FileInfo> deleteFiles = GetFilesToDelete(folderPath, daysBack);
        if (!deleteFiles.Any())
        {
            await OpenDialog.ShowMessage(dialogName,
                $"Files older than {daysBack} day{(daysBack > 1 ? "s" : "")} not found in\n{folderPath}");
            return;
        }

        object ob = await OpenDialog.ShowYesNoCancel(dialogName, $"Delete {deleteFiles.Count} files from {folderPath}?",
            true, DialogIcon.HelpCircle) ?? DialogResult.Cancel;
        if ((DialogResult)ob != DialogResult.OK) return;

        _ = DialogHost.Show(new ProgressBar());
        await Task.Run(() => DeleteFiles(deleteFiles, progress, recycleOption));
        DeleteEmptySubdirectories(folderPath, progress);
        DialogHost.Close(dialogName);
    }

    void DeleteEmptySubdirectories(string parentDirectory, IProgress<string> progress)
    {
        foreach (string directory in Directory.GetDirectories(parentDirectory))
        {
            DeleteEmptySubdirectories(directory, progress);
            if (Directory.EnumerateFileSystemEntries(directory).Any()) continue;
            progress.Report($"Delete folder: {directory}".TrimLongString(50));
            Directory.Delete(directory, false);
        }
    }

    void DeleteFiles(List<FileInfo> deleteFiles, IProgress<string> progress, RecycleOption recycleOption)
    {
        foreach (FileInfo file in deleteFiles)
        {
            try
            {
                progress?.Report($"Delete file: {file.Name}".TrimLongString(50));
                FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, recycleOption);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    List<FileInfo> GetFilesToDelete(string folderPath, int daysBack) => Directory
        .GetFiles(folderPath, "*", SearchOption.AllDirectories).Select(f => new FileInfo(f)).Where(f =>
            !f.Attributes.HasFlag(FileAttributes.Hidden) && f.CreationTime < DateTime.Now.AddDays(-1 * daysBack))
        .ToList();
}