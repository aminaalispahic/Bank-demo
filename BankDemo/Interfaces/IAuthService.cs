using System;
using BankDemo.DTOs;

namespace BankDemo.Interfaces
{
    public interface IAuthService
    {
        Task<RegisterResult> RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
        Task<RegisterResult> CreateStaffAsync(CreateStaffRequestDto request);
    }
}