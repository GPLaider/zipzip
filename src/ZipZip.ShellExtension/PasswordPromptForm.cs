using System.Drawing;
using System.Windows.Forms;

namespace ZipZip.ShellExtension;

internal sealed class PasswordPromptForm : Form
{
    private readonly TextBox _passwordTextBox;

    private PasswordPromptForm(string message)
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "ZipZip 암호 입력";
        ClientSize = new Size(420, 164);
        Padding = new Padding(14);

        var messageLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            Text = message,
        };

        var passwordLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(0, 8, 0, 0),
            Text = "암호",
        };

        _passwordTextBox = new TextBox
        {
            Dock = DockStyle.Top,
            UseSystemPasswordChar = true,
        };

        var buttonsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 38,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false,
        };

        var confirmButton = new Button
        {
            DialogResult = DialogResult.OK,
            Text = "확인",
            Width = 92,
        };

        var cancelButton = new Button
        {
            DialogResult = DialogResult.Cancel,
            Text = "취소",
            Width = 92,
        };

        buttonsPanel.Controls.Add(confirmButton);
        buttonsPanel.Controls.Add(cancelButton);

        Controls.Add(buttonsPanel);
        Controls.Add(_passwordTextBox);
        Controls.Add(passwordLabel);
        Controls.Add(messageLabel);

        AcceptButton = confirmButton;
        CancelButton = cancelButton;

        Shown += (_, _) => _passwordTextBox.Focus();
    }

    public static string? ShowDialog(IWin32Window owner, string message)
    {
        using var dialog = new PasswordPromptForm(message);
        return dialog.ShowDialog(owner) == DialogResult.OK
            ? dialog._passwordTextBox.Text
            : null;
    }
}
