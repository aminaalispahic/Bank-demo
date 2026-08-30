using System;

namespace BankDemo.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int FromAccountId { get; set; }
        public Account FromAccount { get; set; } = null!;
        public int ToAccountId { get; set; }
        public Account ToAccount { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}