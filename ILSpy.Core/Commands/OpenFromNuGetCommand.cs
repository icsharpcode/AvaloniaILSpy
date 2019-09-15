// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.


using System.Collections.Generic;
using Avalonia.Controls;
using ICSharpCode.ILSpy.Properties;
using NuGet.Common;

namespace ICSharpCode.ILSpy
{
    [ExportMainMenuCommand(Menu = nameof(Resources._File), Header = nameof(Resources.OpenFrom_GAC), MenuIcon = "Images/AssemblyListGAC.png", MenuCategory = nameof(Resources.Open), MenuOrder = 1)]
    sealed class OpenFromNuGetCommand : SimpleCommand
	{
		public override async void Execute(object parameter)
		{
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Directory = NuGetEnvironment.GetFolderPath(NuGetFolderPath.NuGetHome);
            dlg.Filters = new List<FileDialogFilter>()
            {
                new FileDialogFilter() { Name = "Nuget Packages (*.nupkg)", Extensions = { "nupkg" }},
                new FileDialogFilter() { Name = ".NET assemblies", Extensions = {"dll","exe", "winmd" }},
                new FileDialogFilter() { Name = "All files", Extensions = { "*" }},
            };
            dlg.AllowMultiple = true;
            var filenames = await dlg.ShowAsync(MainWindow.Instance);
            if (filenames != null && filenames.Length > 0)
            {
                MainWindow.Instance.OpenFiles(filenames);
            }

   //         var dlg = new OpenFromNuGetDialog();
			//dlg.Owner = MainWindow.Instance;
			//if (await dlg.ShowDialog<bool>() == true)
				//OpenFiles(dlg.SelectedFileNames);
		}


	}
}
