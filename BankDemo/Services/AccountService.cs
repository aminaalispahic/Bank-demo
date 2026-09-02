using System;
using BankDemo.DTOs;
using BankDemo.Interfaces;

namespace BankDemo.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IAccountRepository accountRepository, ITransactionRepository transactionRepository, ILogger<AccountService> logger)
        {
            _accountRepository = accountRepository;
            _transactionRepository = transactionRepository;
            _logger = logger;
        }

        public async Task<List<AccountDto>> GetUserAccountsAsync(int userId)
        {
            var accounts = await _accountRepository.GetByUserIdAsync(userId);

            return accounts.Select(a => new AccountDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                Balance = a.Balance
            }).ToList();
        }

        public async Task<TransferResult> TransferAsync(int userId, int fromAccountId, TransferRequestDto request)
        {
            var fromAccount = await _accountRepository.GetByIdAsync(fromAccountId);

            if (fromAccount == null || fromAccount.UserId != userId)
            {
                _logger.LogWarning("Pokušaj transfera sa računa koji ne pripada korisniku. UserId: {UserId}, PokušanRačun: {AccountId}", userId, fromAccountId);
                return new TransferResult { Success = false, ErrorMessage = "Nemate pristup ovom računu." };
            }

            var toAccount = await _accountRepository.GetByIdAsync(request.ToAccountId);

            if (toAccount == null)
            {
                return new TransferResult { Success = false, ErrorMessage = "Odredišni račun ne postoji." };
            }

            if (request.Amount <= 0)
            {
                return new TransferResult { Success = false, ErrorMessage = "Iznos mora biti pozitivan." };
            }

            if (fromAccount.Balance < request.Amount)
            {
                return new TransferResult { Success = false, ErrorMessage = "Nedovoljno sredstava." };
            }

            fromAccount.Balance -= request.Amount;
            toAccount.Balance += request.Amount;

            await _transactionRepository.AddAsync(new Models.Transaction
            {
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = request.Amount
            });

            await _accountRepository.SaveChangesAsync();

            _logger.LogInformation("Transfer izvršen: {Amount} sa računa {FromAccount} na račun {ToAccount}",
    request.Amount, fromAccount.AccountNumber, toAccount.AccountNumber);

            return new TransferResult { Success = true };
        }

        public async Task CreateAccountAsync(CreateAccountRequestDto request)
        {
            var account = new Models.Account
            {
                UserId = request.UserId,
                AccountNumber = request.AccountNumber,
                Balance = request.InitialBalance
            };

            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveChangesAsync();
        }
    }
}