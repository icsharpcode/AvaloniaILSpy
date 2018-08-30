using Avalonia;
using System.IO;
using System.Reflection;
using Avalonia.Logging.Serilog;

namespace ICSharpCode.ILSpy
{
    static class Program
    {
        
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            BuildAvaloniaApp().Start<MainWindow>();
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