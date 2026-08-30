using System;
namespace BankDemo.DTOs
{
    public class CreateAccountRequestDto
    {
        public int UserId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
    }
}