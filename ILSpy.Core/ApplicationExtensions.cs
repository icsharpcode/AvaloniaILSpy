using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

internal static class ApplicationExtensions
{
	public static Window GetMainWindow(this Application application) =>
		application.GetDesktopLifetime().MainWindow;

	public static void Exit(this Application application) =>
		application.GetDesktopLifetime().Shutdown();

	public static IClassicDesktopStyleApplicationLifetime GetDesktopLifetime(this Application application)
	{
		var lifetime = application.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
		Debug.Assert(lifetime != null);

		return lifetime;
	}
}
