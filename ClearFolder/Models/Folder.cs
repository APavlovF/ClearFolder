using PropertyChanged;

namespace ClearFolder.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Folder
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
