using ClearFolder.Dialogs;
using ClearFolder.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClearFolder.Services;

public class DialogService : IDialogService
{
    public async Task<bool> Confirm(string message, string dialogName, DialogIcon icon)
    {
        object? result = await ShowYesNoCancel(dialogName, message, true, icon);
        
        return result is DialogResult.OK;
    }

    public string GetFolder()
    {
        OpenFolderDialog dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName)) return string.Empty;

        return dialog.FolderName;
    }

    public async Task ShowMessage(string dialogName, string message)
    {
        MessageViewModel viewModel = new(message);
        MessageDialog messageDialog = new() { DataContext = viewModel };
        await DialogHost.Show(messageDialog, dialogName);
    }

    public async Task<object?> ShowYesNoCancel(string dialogName, string question, bool useOkCancel,
        DialogIcon icon)
    {
        YesNoCancelDialogViewModel viewModel = new(question, useOkCancel, icon);
        YesNoCancelDialog dialog = new() { DataContext = viewModel };
        return await DialogHost.Show(dialog, dialogName);
    }
}