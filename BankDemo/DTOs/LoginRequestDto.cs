using System;

namespace BankDemo.DTOs
{
    public class LoginRequestDto
    {
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }
}