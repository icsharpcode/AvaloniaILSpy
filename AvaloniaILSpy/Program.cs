using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Avalonia;
using Avalonia.Logging.Serilog;

namespace AvaloniaILSpy
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            BuildAvaloniaApp().Start<MainWindow>();
        }

        public static AppBuilder BuildAvaloniaApp()
                                => AppBuilder.Configure<App>()
#if DEBUG
                                            .LogToDebug()
#endif
                                            .UsePlatformDetect();


    }
}
