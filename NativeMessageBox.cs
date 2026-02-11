using System.Runtime.InteropServices;

internal static class NativeMessageBox
{

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    public enum MessageBoxResult
    {
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    public static MessageBoxResult ShowMessage(string message, string Title = "", bool showYesNo = false)
    {
        return ShowMessageAsync(message, Title, showYesNo).Result;
    }

    public static async Task<MessageBoxResult> ShowMessageAsync(string message, string Title = "", bool showYesNo = false)
    {
        uint uType = showYesNo ? (0x00000004u | 0x00000020u) : 0x00000000u;  // MB_YESNO ve MB_ICONQUESTION

        return await Task.Run(() => (MessageBoxResult)MessageBox(IntPtr.Zero, message, Title, uType));
    }

}
