using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

class ClipboardListener : Form
{
    private static AutoResetEvent clipboardEvent = new AutoResetEvent(false);
    private static string clipboardText = "";

    public static string GetClipboardTextFast()
    {
        clipboardEvent.WaitOne(); // Wait for clipboard update
        return clipboardText;
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_CLIPBOARDUPDATE = 0x031D;
        if (m.Msg == WM_CLIPBOARDUPDATE)
        {
            clipboardText = GetTextFromClipboard();
            clipboardEvent.Set(); // Signal that clipboard has been updated
        }
        base.WndProc(ref m);
    }

    private static string GetTextFromClipboard()
    {
        if (Clipboard.ContainsText())
            return Clipboard.GetText();
        return string.Empty;
    }

    public static void Start()
    {
        Application.Run(new ClipboardListener());
    }
}