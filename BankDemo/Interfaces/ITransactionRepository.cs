using System;

using BankDemo.Models;

namespace BankDemo.Interfaces
{
    public interface ITransactionRepository
    {
        Task AddAsync(Transaction transaction);
        Task<List<Transaction>> GetByUserIdAsync(int userId);
        Task SaveChangesAsync();
    }
}