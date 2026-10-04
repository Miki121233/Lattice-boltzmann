using System.Runtime.ExceptionServices;

namespace LatticeBoltzmann.App.Tests;

/// <summary>Runs WinForms code on a dedicated single-threaded-apartment thread.</summary>
internal static class StaThread
{
    public static void Run(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        ExceptionDispatchInfo? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        captured?.Throw();
    }
}
