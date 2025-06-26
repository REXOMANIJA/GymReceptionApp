// FILE: ViewModels/MainViewModel.cs
using GymReceptionApp.Models;
using GymReceptionApp.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GymReceptionApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private ObservableCollection<Member> _allMembers;
        private List<PaymentRecord> _allPaymentRecords;
        private readonly string _dataFilePath;
        private readonly string _paymentsLogFilePath;

        public ICollectionView FilteredMembers { get; }
        public ObservableCollection<PaymentPlan> PaymentPlans { get; set; }

        private string _searchText;
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); FilteredMembers.Refresh(); } }

        public MainViewModel()
        {
            // Define paths for our data files
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "GymReceptionApp");
            Directory.CreateDirectory(appFolder);
            _dataFilePath = Path.Combine(appFolder, "members.json");
            _paymentsLogFilePath = Path.Combine(appFolder, "payments_log.json");

            LoadPaymentPlans();
            LoadPaymentsLog();
            LoadMembers(); // Also checks for expired subscriptions

            FilteredMembers = CollectionViewSource.GetDefaultView(_allMembers);
            FilteredMembers.Filter = FilterMembers;

            // Register all commands
            OpenInfoCommand = new RelayCommand(OpenMemberInfo);
            HandleAppointmentClickCommand = new RelayCommand(HandleAppointmentClick);
            OpenDebtPopupCommand = new RelayCommand(OpenDebtPopup);
            ConfirmDebtUpdateCommand = new RelayCommand(ConfirmDebtUpdate);
            ClearDebtCommand = new RelayCommand(ClearDebt);
            CancelDebtUpdateCommand = new RelayCommand(p => IsDebtPopupOpen = false);
            OpenAddMemberPopupCommand = new RelayCommand(OpenAddMemberPopup);
            ConfirmAddMemberCommand = new RelayCommand(ConfirmAddMember);
            CancelAddMemberCommand = new RelayCommand(p => IsAddMemberPopupOpen = false);
            OpenPaymentPopupCommand = new RelayCommand(OpenPaymentPopup);
            ProcessPaymentCommand = new RelayCommand(ProcessPayment);
            CancelPaymentPopupCommand = new RelayCommand(p => IsPaymentPopupOpen = false);
        }

        #region --- Data Persistence ---

        private void LoadPaymentPlans()
        {
            try
            {
                string json = File.ReadAllText("payment_plans.json");
                PaymentPlans = JsonConvert.DeserializeObject<ObservableCollection<PaymentPlan>>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load payment plans. Make sure 'payment_plans.json' exists and is set to 'Copy if newer'.\nError: {ex.Message}", "Error");
                PaymentPlans = new ObservableCollection<PaymentPlan>();
            }
        }

        private void LoadPaymentsLog()
        {
            if (File.Exists(_paymentsLogFilePath))
            {
                string json = File.ReadAllText(_paymentsLogFilePath);
                _allPaymentRecords = JsonConvert.DeserializeObject<List<PaymentRecord>>(json);
            }
            else
            {
                _allPaymentRecords = new List<PaymentRecord>();
            }
        }

        private void SavePaymentsLog()
        {
            string json = JsonConvert.SerializeObject(_allPaymentRecords, Formatting.Indented);
            File.WriteAllText(_paymentsLogFilePath, json);
        }

        private void LoadMembers()
        {
            if (File.Exists(_dataFilePath))
            {
                string json = File.ReadAllText(_dataFilePath);
                _allMembers = JsonConvert.DeserializeObject<ObservableCollection<Member>>(json);

                // Check for expired subscriptions
                foreach (var member in _allMembers)
                {
                    if (member.SubscriptionExpiryDate.HasValue && member.SubscriptionExpiryDate.Value < DateTime.Today)
                    {
                        member.Appointments = 0;
                        member.SubscriptionExpiryDate = null; // Clear the expiry date
                    }
                }
            }
            else
            {
                _allMembers = new ObservableCollection<Member>();
                LoadSampleMembers();
                SaveMembers();
            }
        }

        private void SaveMembers()
        {
            string json = JsonConvert.SerializeObject(_allMembers, Formatting.Indented);
            File.WriteAllText(_dataFilePath, json);
        }

        // Central handler to save data and refresh the UI
        private void HandleMemberDataChange()
        {
            SaveMembers();
            FilteredMembers.Refresh();
        }

        private bool FilterMembers(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var member = obj as Member;
            if (member == null) return false;
            return member.FullName.ToLower().Contains(SearchText.ToLower()) || member.Id.ToString().Contains(SearchText);
        }
        #endregion

        #region --- Check-in Logic (Refined) ---

        public RelayCommand HandleAppointmentClickCommand { get; }
        private void HandleAppointmentClick(object parameter)
        {
            if (parameter is Member member)
            {
                // Check today's status
                member.ActivityLog.TryGetValue(DateTime.Today, out DayStatus currentStatus);

                // Case 1: Already fully checked in today. Do nothing.
                if (currentStatus == DayStatus.CheckedIn || currentStatus == DayStatus.PaidAndCheckedIn)
                {
                    MessageBox.Show("Member has already been checked in for today.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Case 2: They have appointments left.
                if (member.Appointments > 0)
                {
                    member.Appointments--; // Always consume an appointment

                    // If they paid today, set the combined status. Otherwise, just set CheckedIn.
                    if (currentStatus == DayStatus.PayDay)
                    {
                        member.ActivityLog[DateTime.Today] = DayStatus.PaidAndCheckedIn;
                    }
                    else
                    {
                        member.ActivityLog[DateTime.Today] = DayStatus.CheckedIn;
                    }
                }
                // Case 3: No appointments left.
                else
                {
                    var result = MessageBox.Show("Member has no appointments left. Continue and log as a debt day?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        member.ActivityLog[DateTime.Today] = DayStatus.DebtDay;
                    }
                }

                HandleMemberDataChange();
            }
        }

        #endregion

        #region --- Payment Popup ---
        private bool _isPaymentPopupOpen;
        public bool IsPaymentPopupOpen { get => _isPaymentPopupOpen; set { _isPaymentPopupOpen = value; OnPropertyChanged(); } }

        private Member _selectedMemberForPayment;
        public Member SelectedMemberForPayment { get => _selectedMemberForPayment; set { _selectedMemberForPayment = value; OnPropertyChanged(); } }

        public RelayCommand OpenPaymentPopupCommand { get; }
        public RelayCommand ProcessPaymentCommand { get; }
        public RelayCommand CancelPaymentPopupCommand { get; }

        private void OpenPaymentPopup(object parameter)
        {
            if (parameter is Member member)
            {
                SelectedMemberForPayment = member;
                IsPaymentPopupOpen = true;
            }
        }

        private void ProcessPayment(object parameter)
        {
            if (SelectedMemberForPayment != null && parameter is PaymentPlan plan)
            {
                // Update member's appointments and subscription
                SelectedMemberForPayment.Appointments = plan.Appointments;
                SelectedMemberForPayment.SubscriptionExpiryDate = DateTime.Today.AddMonths(1);

                // CHANGE: Only mark it as PayDay. Do NOT check them in yet.
                // The check-in happens when they click the check-in button.
                SelectedMemberForPayment.ActivityLog[DateTime.Today] = DayStatus.PayDay;

                // Create a payment record for analytics
                var record = new PaymentRecord
                {
                    PaymentDate = DateTime.Now,
                    MemberId = SelectedMemberForPayment.Id,
                    AmountPaid = plan.Price,
                    PlanName = plan.Name
                };
                _allPaymentRecords.Add(record);
                SavePaymentsLog();

                HandleMemberDataChange(); // Save member changes and refresh UI
                IsPaymentPopupOpen = false; // Close popup
                MessageBox.Show($"{SelectedMemberForPayment.FullName} has successfully subscribed to the {plan.Name}.", "Payment Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region --- Existing Code (with minor changes) ---
        public RelayCommand OpenInfoCommand { get; }
        public RelayCommand OpenDebtPopupCommand { get; }
        public RelayCommand ConfirmDebtUpdateCommand { get; }
        public RelayCommand CancelDebtUpdateCommand { get; }
        public RelayCommand OpenAddMemberPopupCommand { get; }
        public RelayCommand ConfirmAddMemberCommand { get; }
        public RelayCommand CancelAddMemberCommand { get; }
        public RelayCommand ClearDebtCommand { get; }

        // Member Info Window
        private void OpenMemberInfo(object parameter)
        {
            if (parameter is Member member)
            {
                var memberInfoVM = new MemberInfoViewModel(member);
                memberInfoVM.DataChanged += HandleMemberDataChange;
                var infoWindow = new MemberInfoWindow { DataContext = memberInfoVM };
                infoWindow.Closed += (s, e) => { memberInfoVM.DataChanged -= HandleMemberDataChange; };
                infoWindow.Show();
            }
        }

        // Add Member Popup
        private bool _isAddMemberPopupOpen;
        public bool IsAddMemberPopupOpen { get => _isAddMemberPopupOpen; set { _isAddMemberPopupOpen = value; OnPropertyChanged(); } }
        private string _newMemberFullName;
        public string NewMemberFullName { get => _newMemberFullName; set { _newMemberFullName = value; OnPropertyChanged(); } }
        private void OpenAddMemberPopup(object parameter) { NewMemberFullName = string.Empty; IsAddMemberPopupOpen = true; }
        private void ConfirmAddMember(object parameter)
        {
            if (string.IsNullOrWhiteSpace(NewMemberFullName))
            {
                MessageBox.Show("Member name cannot be empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int newId = _allMembers.Any() ? _allMembers.Max(m => m.Id) + 1 : 101;
            var newMember = new Member
            {
                Id = newId,
                FullName = NewMemberFullName,
                Appointments = 0,
                Debt = 0
            };
            _allMembers.Add(newMember);
            HandleMemberDataChange();
            MessageBox.Show($"Member '{newMember.FullName}' added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            IsAddMemberPopupOpen = false;
        }

        // Debt Popup
        private bool _isDebtPopupOpen;
        public bool IsDebtPopupOpen { get => _isDebtPopupOpen; set { _isDebtPopupOpen = value; OnPropertyChanged(); } }
        private Member _selectedMemberForDebt;
        public Member SelectedMemberForDebt { get => _selectedMemberForDebt; set { _selectedMemberForDebt = value; OnPropertyChanged(); } }
        private string _newDebtValue;
        public string NewDebtValue { get => _newDebtValue; set { _newDebtValue = value; OnPropertyChanged(); } }
        private void OpenDebtPopup(object parameter) { if (parameter is Member member) { SelectedMemberForDebt = member; NewDebtValue = member.Debt.ToString("F2"); IsDebtPopupOpen = true; } }
        private void ConfirmDebtUpdate(object parameter)
        {
            if (SelectedMemberForDebt != null && decimal.TryParse(NewDebtValue, out decimal newDebt))
            {
                SelectedMemberForDebt.Debt = newDebt;
                IsDebtPopupOpen = false;
                HandleMemberDataChange();
            }
            else { MessageBox.Show("Please enter a valid value for the debt.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void ClearDebt(object parameter)
        {
            if (SelectedMemberForDebt != null)
            {
                SelectedMemberForDebt.Debt = 0;
                IsDebtPopupOpen = false;
                HandleMemberDataChange();
                MessageBox.Show($"Debt for member {SelectedMemberForDebt.FullName} has been cleared.", "Debt Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Sample Data
        private void LoadSampleMembers()
        {
            _allMembers.Add(new Member { Id = 101, FullName = "Marko Markovic", Appointments = 12, Debt = 0, SubscriptionExpiryDate = DateTime.Today.AddDays(15) });
            _allMembers.Add(new Member { Id = 102, FullName = "Jelena Jovanovic", Appointments = 0, Debt = 50.00m });
            _allMembers.Add(new Member { Id = 103, FullName = "Petar Petrovic", Appointments = 20, Debt = 15.50m, SubscriptionExpiryDate = DateTime.Today.AddDays(25) });
        }
        #endregion
    }
}