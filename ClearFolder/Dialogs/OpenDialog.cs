using MaterialDesignThemes.Wpf;
using System.Threading.Tasks;

namespace ClearFolder.Dialogs;

public static class OpenDialog
{
    public static async Task ShowMessage(string dialogName, string message)
    {
        MessageViewModel viewModel = new(message);
        MessageDialog messageDialog = new() { DataContext = viewModel };
        await DialogHost.Show(messageDialog, dialogName);
    }

    public static async Task<object> ShowYesNoCancel(string dialogName, string question, bool useOkCancel)
    {
        YesNoCancelDialogViewModel viewModel = new(question, useOkCancel);
        YesNoCancelDialog dialog = new() { DataContext = viewModel };
        return await DialogHost.Show(dialog, dialogName);
    }
}