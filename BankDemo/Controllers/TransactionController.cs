using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BankDemo.Interfaces;

namespace BankDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionsController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyTransactions()
        {
            int userId = GetUserId();
            var result = await _transactionService.GetUserTransactionsAsync(userId);
            return Ok(result);
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!.Value);
        }
    }
}
