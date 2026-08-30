using System;

using BankDemo.DTOs;

namespace BankDemo.Interfaces
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetUserAccountsAsync(int userId);
        Task<TransferResult> TransferAsync(int userId, int fromAccountId, TransferRequestDto request);
        Task CreateAccountAsync(CreateAccountRequestDto request);
    }
}