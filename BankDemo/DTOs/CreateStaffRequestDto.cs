using System;

using BankDemo.Enums;

namespace BankDemo.DTOs
{
    public class CreateStaffRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}