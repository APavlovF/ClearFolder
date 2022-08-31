using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PropertyChanged;

namespace ClearFolder.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Folder
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public bool SelectEnabled => DeleteFilesCount != 0;

        [OnChangedMethod(nameof(SetFilesInfo))]
        public int DaysBack { get; set; }

        [OnChangedMethod(nameof(SetFilesInfo))]
        public int DeleteFilesCount { get; set; }
        public string DeleteFilesSize { get; set; }
        public long DeleteFilesBytes { get; set; }

        void SetFilesInfo()
        {
            if (string.IsNullOrWhiteSpace(Name)) return;
            List<FileInfo> fis = Directory.GetFiles(Name, "*", SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Where(f => !f.Attributes.HasFlag(FileAttributes.Hidden)
                            && f.CreationTime < DateTime.Now.AddDays(-1 * DaysBack)).ToList();
            long size = fis.Sum(fi => fi.Length);
            DeleteFilesBytes = size;
            DeleteFilesSize = FormatFileSize(size);
            DeleteFilesCount = fis.Count;
            if (DeleteFilesCount == 0)
            {
                IsSelected = false;
            }
        }

        public static string FormatFileSize(long bytes)
        {
            const int unit = 1024;
            if (bytes < unit) { return $"{bytes} B"; }
            int exp = (int)(Math.Log(bytes) / Math.Log(unit));
            return $"{bytes / Math.Pow(unit, exp):F2} {"KMGTPE"[exp - 1]}B";
        }
    }
}
