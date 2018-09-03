using Avalonia;
using Avalonia.Logging.Serilog;
using System;
using System.IO;
using System.Reflection;

namespace ICSharpCode.ILSpy
{
    static class Program
    {
        
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            try
            {
                BuildAvaloniaApp().Start<MainWindow>();
            }
            catch (Exception exception)
            {
			    MessageBox.Show(exception.ToString(), "Sorry, we crashed");
            }
        }

        /// <summary>
        /// This method is needed for IDE previewer infrastructure
        /// </summary>
        public static AppBuilder BuildAvaloniaApp()
                                          => AppBuilder.Configure<App>()
#if DEBUG
                                            .LogToDebug()
#endif
                                            .UsePlatformDetect();
    }
}