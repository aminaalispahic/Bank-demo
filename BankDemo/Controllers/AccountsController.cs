using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BankDemo.DTOs;
using BankDemo.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost]
        [Authorize(Roles = "Referent,Admin")]
        public async Task<IActionResult> CreateAccount(CreateAccountRequestDto request)
        {
            await _accountService.CreateAccountAsync(request);
            return Ok("Račun uspješno kreiran.");
        }

        [HttpGet("search")]
        public IActionResult SearchByAccountNumber(string accountNumber)
        {
            var connectionString = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>().GetConnectionString("Default");

            using var conn = new Npgsql.NpgsqlConnection(connectionString);
            conn.Open();

            // VULN: SQL Injection - direktna konkatenacija stringa u upit, bez parametrizacije
            var query = $"SELECT * FROM \"Accounts\" WHERE \"AccountNumber\" = '{accountNumber}'";
            using var cmd = new Npgsql.NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            var results = new List<object>();
            while (reader.Read())
            {
                results.Add(new { Id = reader["Id"], AccountNumber = reader["AccountNumber"] });
            }

            return Ok(results);
        }

        // VULN: Nedostaje [Authorize] - endpoint dostupan bilo kome, bez tokena
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllAccounts()
        {
            var allAccounts = await _accountService.GetUserAccountsAsync(userId: 0);
            return Ok(allAccounts);
        }


    }
}