// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using Avalonia;
using Avalonia.Markup;
using System.Globalization;
using Portable.Xaml.Markup;
using Avalonia.Data.Converters;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Data;

namespace ICSharpCode.TreeView
{
	public class BoolConverters 
	{
		public static readonly IValueConverter Inverse = new FuncValueConverter<bool,bool>((b) => !b);
	}
}
