// FILE: Models/PaymentPlan.cs
namespace GymReceptionApp.Models
{
    public class PaymentPlan
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Appointments { get; set; }
    }
}