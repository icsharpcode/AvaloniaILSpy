using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Logging.Serilog;

namespace AvaloniaILSpy
{
	class Program
	{
		static void Main(string[] args)
		{
			AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToDebug()
				.Start<MainWindow>();
		}
	}
}
