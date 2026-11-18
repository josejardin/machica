using GymRegistrationApp.Forms;
using GymRegistrationApp.Services;

namespace GymRegistrationApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Global error handlers to prevent unhandled app crashes
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
        {
            Console.Error.WriteLine($"[ThreadException] {e.Exception}");
            MessageBox.Show(
                $"An error occurred:\n{e.Exception.Message}\n\nThe system recorded this event and will keep running.",
                "System Error Notice",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Console.Error.WriteLine($"[FatalException] {ex}");
            }
        };

        try
        {
            ApplicationConfiguration.Initialize();
            DataStore.Initialize();
            EmbeddedAttendanceServer.Start();

            Application.ApplicationExit += (_, _) =>
            {
                try { CloudflareTunnelService.StopTunnel(); } catch { }
                try { EmbeddedAttendanceServer.Stop(); } catch { }
            };

            // Test database connection to XAMPP MySQL
            bool dbConnected = DatabaseConnection.TestConnection();
            if (!dbConnected)
            {
                MessageBox.Show(
                    "Note: Could not connect to the MySQL database.\n\n" +
                    "Make sure XAMPP MySQL is started if you want to sync records to MySQL.\n\n" +
                    "The system will run normally in Offline Local Storage mode.",
                    "Database Notice",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            Application.Run(new LoginForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup error: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
