using System;
namespace BankDemo.DTOs
{
    public class RegisterResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
    }
}