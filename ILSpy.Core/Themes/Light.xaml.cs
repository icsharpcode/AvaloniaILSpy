using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace ICSharpCode.ILSpy.Themes
{
    /// <summary>
    /// The light theme 
    /// </summary>
    public class Light : Styles
    {
        public Light()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
