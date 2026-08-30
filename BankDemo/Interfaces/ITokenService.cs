using System;

using BankDemo.Models;

namespace BankDemo.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}