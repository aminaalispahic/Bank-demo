using System;

using BankDemo.DTOs;
using BankDemo.Interfaces;

namespace BankDemo.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;

        public TransactionService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<List<TransactionDto>> GetUserTransactionsAsync(int userId)
        {
            var transactions = await _transactionRepository.GetByUserIdAsync(userId);

            return transactions.Select(t => new TransactionDto
            {
                Id = t.Id,
                FromAccountNumber = t.FromAccount.AccountNumber,
                ToAccountNumber = t.ToAccount.AccountNumber,
                Amount = t.Amount,
                Timestamp = t.Timestamp
            }).ToList();
        }
    }
}