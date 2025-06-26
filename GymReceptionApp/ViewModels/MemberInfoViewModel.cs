using GymReceptionApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace GymReceptionApp.ViewModels
{
    // A small ViewModel for each day button in the calendar
    public class CalendarDayViewModel : BaseViewModel
    {
        public int Day { get; }
        public bool IsInCurrentMonth { get; }
        private DayStatus _status;

        public DayStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public CalendarDayViewModel(int day, bool isInCurrentMonth, DayStatus status)
        {
            Day = day;
            IsInCurrentMonth = isInCurrentMonth;
            Status = status;
        }
    }

    public class MemberInfoViewModel : BaseViewModel
    {
        private Member _member;
        public event Action DataChanged;
        public Member Member
        {
            get => _member;
            set { _member = value; OnPropertyChanged(); }
        }

        private DateTime _displayedMonth;
        public DateTime DisplayedMonth
        {
            get => _displayedMonth;
            set { _displayedMonth = value; OnPropertyChanged(); UpdateCalendar(); }
        }

        public ObservableCollection<CalendarDayViewModel> Days { get; } = new ObservableCollection<CalendarDayViewModel>();

        // --- Commands ---
        public ICommand ChangeMonthCommand { get; }
        public ICommand SetDayStatusCommand { get; }

        public MemberInfoViewModel(Member member)
        {
            Member = member;
            DisplayedMonth = DateTime.Today;

            ChangeMonthCommand = new RelayCommand(p => DisplayedMonth = DisplayedMonth.AddMonths(int.Parse((string)p)));
            SetDayStatusCommand = new RelayCommand(SetDayStatus);

            UpdateCalendar();
        }

        private void UpdateCalendar()
        {
            Days.Clear();
            var firstDayOfMonth = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(DisplayedMonth.Year, DisplayedMonth.Month);

            // DayOfWeek in .NET is Sunday=0, Monday=1, ...
            int startingOffset = (int)firstDayOfMonth.DayOfWeek;

            // Add blank days for the preceding month
            for (int i = 0; i < startingOffset; i++)
            {
                Days.Add(new CalendarDayViewModel(0, false, DayStatus.None));
            }

            // Add days for the current month
            for (int i = 1; i <= daysInMonth; i++)
            {
                var date = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, i);
                var status = Member.ActivityLog.ContainsKey(date) ? Member.ActivityLog[date] : DayStatus.None;
                Days.Add(new CalendarDayViewModel(i, true, status));
            }
        }

        private void SetDayStatus(object parameter)
        {
            if (parameter is object[] values && values.Length == 2)
            {
                if (values[0] is CalendarDayViewModel dayVM && values[1] is DayStatus newStatus)
                {
                    var date = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, dayVM.Day);

                    if (newStatus == DayStatus.None)
                    {
                        if (Member.ActivityLog.ContainsKey(date))
                        {
                            Member.ActivityLog.Remove(date);
                        }
                    }
                    else
                    {
                        Member.ActivityLog[date] = newStatus;
                    }

                    dayVM.Status = newStatus;

                    // CHANGE 2: Raise the event to notify listeners that data has been modified.
                    DataChanged?.Invoke();
                }
            }
        }
    }
}