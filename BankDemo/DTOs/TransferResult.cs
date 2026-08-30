using System;

namespace BankDemo.DTOs
{
    public class TransferResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}