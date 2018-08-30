// Copyright (c) 2018 Jeffrey

using System;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ICSharpCode.ILSpy
{
	public static class MessageBox
	{

		public static Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
		{
			return Show(Avalonia.Application.Current.MainWindow, messageBoxText, caption, button, icon, defaultResult, MessageBoxOptions.None);
		}

		public static Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
		{
			return Show(Avalonia.Application.Current.MainWindow, messageBoxText, caption, button, icon,  MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static Task<MessageBoxResult> Show(string messageBoxText, string caption, MessageBoxButton button)
		{
			return Show(Avalonia.Application.Current.MainWindow, messageBoxText, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static Task<MessageBoxResult> Show(string messageBoxText, string caption)
		{
			return Show(Avalonia.Application.Current.MainWindow, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static Task<MessageBoxResult> Show(string messageBoxText)
		{
			return Show(Avalonia.Application.Current.MainWindow, messageBoxText, "Message", MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static Task<MessageBoxResult> Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			//TODO: message box
			//var win = new Window();
			//win.Owner = owner;
			//win.Title = caption;
			//win.Content = messageBoxText;
			//win.Topmost = true;
			//return await win.ShowDialog<MessageBoxResult>();
			Debug.WriteLine(caption);
            return Task.FromResult(defaultResult);
		}
	}

	public enum MessageBoxOptions
	{
		None
	}
	
	public enum MessageBoxResult
	{
		None = 0,
		OK = 1,
		Cancel = 2,
		Yes = 6,
		No = 7
	}

	public enum MessageBoxImage
	{
		None = 0,

		Hand = 16,
		Stop = 16,
		Error = 16,

		Question = 32,

		Exclamation = 48,
		Warning = 48,

		Asterisk = 64,
		Information = 64
	}

	public enum MessageBoxButton
	{
		OK = 0,
		OKCancel = 1,
		YesNoCancel = 3,
		YesNo = 4
	}
}