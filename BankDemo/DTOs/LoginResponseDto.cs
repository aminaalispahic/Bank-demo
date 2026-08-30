using System;

namespace BankDemo.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
    }
}