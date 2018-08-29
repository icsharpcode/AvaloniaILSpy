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

namespace AvaloniaILSpy.Controls
{
	public class BoolConverters 
	{
		public static readonly IValueConverter Inverse = new FuncValueConverter<bool,bool>((b) => !b);

		public static readonly IMultiValueConverter Conditional = new ConditionalConverter();

		class ConditionalConverter :IMultiValueConverter
		{
			public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
			{
				if(BindingNotification.ExtractValue(values[0]) == AvaloniaProperty.UnsetValue) 
				{
					return AvaloniaProperty.UnsetValue;
				}
				var condition = (bool)BindingNotification.ExtractValue(values[0]);
				var trueResult = (object)values[1];
				var falseResult = (object)values[2];
				return condition ? trueResult : falseResult;
			}

			public object[] ConvertBack(
				object value, Type[] targetTypes, object parameter, CultureInfo culture)
			{
				throw new NotSupportedException();
			}
		}
	}
}
