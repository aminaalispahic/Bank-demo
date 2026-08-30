using System;

using Moq;
using Xunit;
using BankDemo.Services;
using BankDemo.Interfaces;
using BankDemo.Models;
using BankDemo.DTOs;

namespace BankDemo.Tests
{
    public class AccountServiceTests
    {
        [Fact]
        public async Task TransferAsync_FromAccountNotOwnedByUser_ReturnsFailure()
        {
            // Arrange - pripremi lažne (mock) zavisnosti
            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockTransactionRepo = new Mock<ITransactionRepository>();

            // Simuliramo: račun sa Id=5 postoji, ali pripada korisniku sa Id=99
            mockAccountRepo.Setup(repo => repo.GetByIdAsync(5))
                .ReturnsAsync(new Account { Id = 5, UserId = 99, Balance = 1000, AccountNumber = "RS999" });

            var service = new AccountService(mockAccountRepo.Object, mockTransactionRepo.Object);

            var request = new TransferRequestDto { ToAccountId = 10, Amount = 100 };

            // Act - napadač (userId=1) pokušava transfer sa računa koji pripada userId=99
            var result = await service.TransferAsync(userId: 1, fromAccountId: 5, request);

            // Assert - očekujemo odbijanje
            Assert.False(result.Success);
            Assert.Equal("Nemate pristup ovom računu.", result.ErrorMessage);
        }
    

    [Fact]
        public async Task TransferAsync_InsufficientBalance_ReturnsFailure()
        {
            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockTransactionRepo = new Mock<ITransactionRepository>();

            // Korisnik 1 ima svoj račun sa balansom 50
            mockAccountRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, UserId = 1, Balance = 50, AccountNumber = "RS001" });
            mockAccountRepo.Setup(repo => repo.GetByIdAsync(2))
                .ReturnsAsync(new Account { Id = 2, UserId = 2, Balance = 500, AccountNumber = "RS002" });

            var service = new AccountService(mockAccountRepo.Object, mockTransactionRepo.Object);

            // Pokušava poslati 100, iako ima samo 50
            var request = new TransferRequestDto { ToAccountId = 2, Amount = 100 };

            var result = await service.TransferAsync(userId: 1, fromAccountId: 1, request);

            Assert.False(result.Success);
            Assert.Equal("Nedovoljno sredstava.", result.ErrorMessage);
        }

        [Fact]
        public async Task TransferAsync_NegativeAmount_ReturnsFailure()
        {
            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockTransactionRepo = new Mock<ITransactionRepository>();

            mockAccountRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, UserId = 1, Balance = 1000, AccountNumber = "RS001" });
            mockAccountRepo.Setup(repo => repo.GetByIdAsync(2))
                .ReturnsAsync(new Account { Id = 2, UserId = 2, Balance = 500, AccountNumber = "RS002" });

            var service = new AccountService(mockAccountRepo.Object, mockTransactionRepo.Object);

            var request = new TransferRequestDto { ToAccountId = 2, Amount = -50 };

            var result = await service.TransferAsync(userId: 1, fromAccountId: 1, request);

            Assert.False(result.Success);
            Assert.Equal("Iznos mora biti pozitivan.", result.ErrorMessage);
        }

        [Fact]
        public async Task TransferAsync_ValidRequest_ReturnsSuccess()
        {
            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockTransactionRepo = new Mock<ITransactionRepository>();

            mockAccountRepo.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(new Account { Id = 1, UserId = 1, Balance = 1000, AccountNumber = "RS001" });
            mockAccountRepo.Setup(repo => repo.GetByIdAsync(2))
                .ReturnsAsync(new Account { Id = 2, UserId = 2, Balance = 500, AccountNumber = "RS002" });

            var service = new AccountService(mockAccountRepo.Object, mockTransactionRepo.Object);

            var request = new TransferRequestDto { ToAccountId = 2, Amount = 100 };

            var result = await service.TransferAsync(userId: 1, fromAccountId: 1, request);

            Assert.True(result.Success);
        }
    }
}