using Avalonia.Controls;
using ICSharpCode.ILSpy.Properties;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[ExportContextMenuEntry(Header = nameof(Resources.CopyName), Icon = "Images/Copy.png", Order = 9999)]
	public class CopyFullyQualifiedNameContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return GetMemberNodeFromContext(context) != null;
		}

		public bool IsEnabled(TextViewContext context) => true;

		public void Execute(TextViewContext context)
		{
			var member = GetMemberNodeFromContext(context)?.Member;
			if (member == null)
				return;

			//// App.Current.Clipboard.SetTextAsync(member.ReflectionName);
			var clipboard = App.Current.GetMainWindow()?.Clipboard;
			clipboard.SetTextAsync(member.ReflectionName);
		}

		private IMemberTreeNode GetMemberNodeFromContext(TextViewContext context)
		{
			return context.SelectedTreeNodes?.Length == 1 ? context.SelectedTreeNodes[0] as IMemberTreeNode : null;
		}
	}
}
