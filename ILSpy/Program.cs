using Avalonia;
using System;
using System.IO;
using System.Reflection;
using Avalonia.Logging;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Dialogs;
using System.Runtime.InteropServices;

namespace ICSharpCode.ILSpy
{
	static class Program
	{
		
		static void Main(string[] args)
		{
			Directory.SetCurrentDirectory(AppContext.BaseDirectory);

			try
			{
				BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
			}
			catch (Exception exception)
			{
				Console.WriteLine("Sorry, we crashed");
				Console.WriteLine(exception.ToString());
				//MessageBox.Show(exception.ToString(), "Sorry, we crashed");
			}
		}


		/// <summary>
		/// This method is needed for IDE previewer infrastructure
		/// </summary>
		public static AppBuilder BuildAvaloniaApp()
		{
			var result = AppBuilder.Configure<App>();


#if DEBUG
			result.LogToTrace();
			Logger.Sink = new ProxyLogSink(Logger.Sink);
#endif

			AppBuilder app = result
				.UsePlatformDetect()
				.With(new X11PlatformOptions
				{
					UseDBusMenu = true
				});

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
			{
				app = app.UseManagedSystemDialogs();
			}

			return app;
		}

		class ProxyLogSink : ILogSink
		{
			private ILogSink sink;

			public ProxyLogSink(ILogSink sink)
			{
				this.sink = sink;
			}

			public bool IsEnabled(LogEventLevel level) => true;

            public bool IsEnabled(LogEventLevel level, string area) => true;

			public void Log(LogEventLevel level, string area, object source, string messageTemplate) =>
				Log(level, area, source, messageTemplate, Array.Empty<object>());

			public void Log<T0>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0) =>
				Log(level, area, source, messageTemplate, new object[] { propertyValue0 });

			public void Log<T0, T1>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1) =>
				Log(level, area, source, messageTemplate, new object[] { propertyValue0, propertyValue1 });

			public void Log<T0, T1, T2>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) =>
				Log(level, area, source, messageTemplate, new object[] { propertyValue0, propertyValue1, propertyValue2 });

			public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
			{
				for (int i = 0; i < propertyValues.Length; i++)
				{
					propertyValues[i] = GetHierachy(propertyValues[i]);
				}
				sink.Log(level, area, source, messageTemplate, propertyValues);
			}

			object GetHierachy(object source)
			{
				if (source is IControl visual)
				{
					List<string> hierachy = new List<string>();
					hierachy.Add(visual.ToString());
					while ((visual = visual.Parent) != null)
					{
						hierachy.Insert(0, visual.ToString());
					}
					return string.Join("/", hierachy);
				}
				return source;
			}
		}
	}
}