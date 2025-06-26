// FILE: Converters/AppointmentsToBrushConverter.cs
using GymReceptionApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GymReceptionApp.Converters
{
    public class AppointmentsToBrushConverter : IMultiValueConverter
    {
        // Define brushes once for performance
        private readonly Brush _successBrush = (Brush)new BrushConverter().ConvertFrom("#28a745");
        private readonly Brush _dangerBrush = (Brush)new BrushConverter().ConvertFrom("#dc3545");
        private readonly Brush _payDayBrush = (Brush)new BrushConverter().ConvertFrom("#FFD700");

        // This is the default color when there is no activity for today
        private readonly Brush _defaultBrush = Brushes.Transparent;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Basic validation: ensure we have the activity log
            if (values == null || values.Length == 0 || values[0] is not Dictionary<DateTime, DayStatus> activityLog)
            {
                return _defaultBrush;
            }

            // The logic now ONLY depends on today's activity log.
            // We don't care about the number of appointments for the color.
            if (activityLog.TryGetValue(DateTime.Today, out DayStatus status))
            {
                switch (status)
                {
                    // If checked-in (with or without payment), the button is green.
                    case DayStatus.CheckedIn:
                    case DayStatus.PaidAndCheckedIn:
                        return _successBrush;

                    // If they just paid but haven't checked in yet, it's gold.
                    case DayStatus.PayDay:
                        return _payDayBrush;

                    // If it's a debt day, it's red.
                    case DayStatus.DebtDay:
                        return _dangerBrush;

                    // If the status is explicitly 'None' or any other value, use default.
                    case DayStatus.None:
                    default:
                        return _defaultBrush;
                }
            }

            // CRUCIAL: If there is NO entry in the log for today, return the default color.
            // This is what fixes the problem.
            return _defaultBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}