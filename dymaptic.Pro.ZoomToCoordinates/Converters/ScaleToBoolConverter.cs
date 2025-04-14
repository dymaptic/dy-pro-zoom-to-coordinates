using System;
using System.Globalization;
using System.Windows.Data;

namespace dymaptic.Pro.ZoomToCoordinates.Converters;

public class ScaleToBoolConverter : IValueConverter
{

	// Determines which radio button for the scale is checked (value is the scale value from the ViewModel while targetType is the value in the View)
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return (Double.Parse((string)parameter) == (double) value);
	}
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return (bool)value ? parameter : Binding.DoNothing;
	}
}
