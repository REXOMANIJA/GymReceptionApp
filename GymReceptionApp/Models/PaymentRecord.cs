// FILE: Models/PaymentRecord.cs
using System;

namespace GymReceptionApp.Models
{
    public class PaymentRecord
    {
        public DateTime PaymentDate { get; set; }
        public int MemberId { get; set; }
        public decimal AmountPaid { get; set; }
        public string PlanName { get; set; }
    }
}