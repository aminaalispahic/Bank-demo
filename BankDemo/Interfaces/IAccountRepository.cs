using System;
using BankDemo.Models;
namespace BankDemo.Interfaces
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetByUserIdAsync(int userId);
        Task<Account?> GetByIdAsync(int accountId);
        Task SaveChangesAsync();
        Task AddAsync(Account account);
    }
}