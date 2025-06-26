using GymReceptionApp.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GymReceptionApp.Converters
{
    public class DayStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DayStatus status)
            {
                switch (status)
                {
                    case DayStatus.CheckedIn:
                        return (Brush)new BrushConverter().ConvertFrom("#28a745"); // Success Green
                    case DayStatus.PayDay:
                        return (Brush)new BrushConverter().ConvertFrom("#FFD700"); // Gym Gold
                    case DayStatus.DebtDay:
                        return (Brush)new BrushConverter().ConvertFrom("#dc3545"); // Danger Red
                    case DayStatus.None:
                    default:
                        // This should be the background color of the calendar day button itself
                        return (Brush)new BrushConverter().ConvertFrom("#2C2C2C");
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}