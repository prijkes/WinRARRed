using System;
using System.Text;
using System.Windows.Forms;
using WinRARRed.Forms;

namespace WinRARRed;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Log.Information(null, "Application starting...");

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(null, ex, "Unhandled exception in application");
            MessageBox.Show($"A fatal error occurred: {ex.Message}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
