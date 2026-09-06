using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BankDemo.DTOs;
using BankDemo.Interfaces;

namespace BankDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountsController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAccounts()
        {
            int userId = GetUserId();
            var accounts = await _accountService.GetUserAccountsAsync(userId);
            return Ok(accounts);
        }

        [HttpPost("{accountId}/transfer")]
        public async Task<IActionResult> Transfer(int accountId, TransferRequestDto request)
        {
            int userId = GetUserId();
            var result = await _accountService.TransferAsync(userId, accountId, request);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Transfer uspješan.");
        }

        private int GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(idClaim!.Value);
        }
        //Smiju samo referent i admin kreirati racun
        [HttpPost]
        [Authorize(Roles = "Referent,Admin")]
        public async Task<IActionResult> CreateAccount(CreateAccountRequestDto request)
        {
            await _accountService.CreateAccountAsync(request);
            return Ok("Račun uspješno kreiran.");
        }
    }
}