using System;

using Microsoft.EntityFrameworkCore;
using BankDemo.Data;
using BankDemo.Models;
using BankDemo.Interfaces;

namespace BankDemo.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly AppDbContext _context;

        public TransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Transaction transaction)
        {
            await _context.Transactions.AddAsync(transaction);
        }

        public async Task<List<Transaction>> GetByUserIdAsync(int userId)
        {
            return await _context.Transactions
                .Where(t => t.FromAccount.UserId == userId || t.ToAccount.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}