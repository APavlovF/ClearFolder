using ClearFolder.Models;
using System.Threading.Tasks;

namespace ClearFolder.Services;

public interface IDialogService
{
    Task<bool> Confirm(string message, string dialogName, DialogIcon icon);

    string GetFolder();

    Task ShowMessage(string dialogName, string message);

    Task<object?> ShowYesNoCancel(string dialogName, string question, bool useOkCancel, DialogIcon icon);
}