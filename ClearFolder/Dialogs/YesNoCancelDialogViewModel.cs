using ClearFolder.Models;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace ClearFolder.Dialogs;

public class YesNoCancelDialogViewModel
{
    public ObservableCollection<DialogButton> Buttons { get; } = [];
    public string IconKind { get; set; }
    public string Question { get; set; }

    public YesNoCancelDialogViewModel(string question, bool useOkCancel, DialogIcon icon)
    {
        IconKind = $"{icon}";
        Question = question;

        if (useOkCancel)
        {
            Buttons.Add(new DialogButton { Content = "Ok", CommandParameter = DialogResult.OK, IsDefault = true });
        }
        else
        {
            Buttons.Add(new DialogButton { Content = "Yes", CommandParameter = DialogResult.Yes, IsDefault = true });
            Buttons.Add(new DialogButton { Content = "No", CommandParameter = DialogResult.No });
        }

        Buttons.Add(new DialogButton { Content = "Cancel", CommandParameter = DialogResult.Cancel, IsCancel = true });
    }
}