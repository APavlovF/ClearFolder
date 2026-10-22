using ClearFolder.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ClearFolder.Services;

public interface IFolderRepository
{
    void AddFolderToRegistry(ObservableCollection<Folder> foldersList);

    ObservableCollection<Folder> GetFolders();

    Task OpenFolder(Folder? folder, string dialogName);
}