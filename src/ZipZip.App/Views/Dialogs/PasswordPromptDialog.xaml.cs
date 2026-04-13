using Microsoft.UI.Xaml.Controls;

namespace ZipZip.App.Views.Dialogs;

public sealed partial class PasswordPromptDialog : ContentDialog
{
    public PasswordPromptDialog()
    {
        InitializeComponent();
    }

    public string Password => PasswordBox.Password;

    public void Initialize(string message)
    {
        MessageTextBlock.Text = message;
    }
}
