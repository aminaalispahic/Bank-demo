using System;

using Microsoft.EntityFrameworkCore;
using BankDemo.Data;
using BankDemo.Models;
using BankDemo.Interfaces;

namespace BankDemo.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _context;

        public AccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Account>> GetByUserIdAsync(int userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<Account?> GetByIdAsync(int accountId)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
        }
    }
}