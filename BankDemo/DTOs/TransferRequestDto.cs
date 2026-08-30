using System;
namespace BankDemo.DTOs
{
    public class TransferRequestDto
    {
        public int ToAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
