// FILE: Models/DayStatus.cs
namespace GymReceptionApp.Models
{
    public enum DayStatus
    {
        None,
        CheckedIn,
        PayDay,
        DebtDay,
        PaidAndCheckedIn // NEW STATUS
    }
}