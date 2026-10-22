using Microsoft.VisualBasic.FileIO;
using System;
using System.Threading.Tasks;

namespace ClearFolder.Services;

public interface IFolderCleaner
{
    Task ClearFolder(string folderPath, int daysBack, string dialogName, IProgress<string> progress,
        RecycleOption recycleOption);
}