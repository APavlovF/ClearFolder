namespace ClearFolder.Models;

public class DialogButton
{
    public string Content { get; set; }
    public object CommandParameter { get; set; }
    public bool IsDefault { get; set; }
    public bool IsCancel { get; set; }
}