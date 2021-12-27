using System;
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
        public int DaysBack { get; set; }
        public int DeleteFilesCount { get; set; }

        void OnDaysBackChanged()
        {
            if (string.IsNullOrWhiteSpace(Name)) return;
            DeleteFilesCount = Directory.GetFiles(Name).Select(f => new FileInfo(f))
                .Count(f => !f.Attributes.HasFlag(FileAttributes.Hidden)
                            && f.CreationTime < DateTime.Now.AddDays(-1 * DaysBack));
        }
    }
}
