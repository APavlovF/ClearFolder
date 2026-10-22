using ClearFolder.Dialogs;
using ClearFolder.Models;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClearFolder.Services;

public class DialogService : IDialogService
{
    public async Task<bool> Confirm(string message, string dialogName, DialogIcon icon)
    {
        object? result = await OpenDialog.ShowYesNoCancel(dialogName, message, true, icon);
        
        return result is DialogResult.OK;
    }

    public string GetFolder()
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName)) return string.Empty;

        return dialog.FolderName;
    }
}