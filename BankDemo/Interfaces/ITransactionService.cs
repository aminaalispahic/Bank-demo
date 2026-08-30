using System;

using BankDemo.DTOs;

namespace BankDemo.Interfaces
{
    public interface ITransactionService
    {
        Task<List<TransactionDto>> GetUserTransactionsAsync(int userId);
    }
}
