using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ICSharpCode.ILSpy.Themes
{
    /// <summary>
    /// The dark theme 
    /// </summary>
    public class Dark : Styles
    {
        public Dark()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
