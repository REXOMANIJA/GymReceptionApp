using GymReceptionApp.ViewModels;
using System;
using System.Collections.Generic;

namespace GymReceptionApp.Models
{
    public class Member : BaseViewModel
    {
        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private decimal _debt;
        public decimal Debt
        {
            get => _debt;
            set { _debt = value; OnPropertyChanged(); }
        }

        private int _appointments;
        public int Appointments
        {
            get => _appointments;
            set { _appointments = value; OnPropertyChanged(); }
        }

        // NEW: Property to track when the subscription ends
        public DateTime? SubscriptionExpiryDate { get; set; }

        public Dictionary<DateTime, DayStatus> ActivityLog { get; set; }

        public Member()
        {
            ActivityLog = new Dictionary<DateTime, DayStatus>();
        }
    }
}