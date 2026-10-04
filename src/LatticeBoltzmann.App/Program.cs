using System.Globalization;

namespace LatticeBoltzmann.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ReportError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                ReportError(ex);
            }
        };
        Application.Run(new MainForm());
    }

    private static void ReportError(Exception ex)
    {
        foreach (var form in Application.OpenForms.OfType<MainForm>())
        {
            if (form.InvokeRequired)
            {
                form.Invoke(form.StopSimulation);
            }
            else
            {
                form.StopSimulation();
            }
        }

        _ = MessageBox.Show(
            string.Create(CultureInfo.CurrentCulture, $"Wystąpił nieoczekiwany błąd: {ex.Message}"),
            "Lattice Boltzmann",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
