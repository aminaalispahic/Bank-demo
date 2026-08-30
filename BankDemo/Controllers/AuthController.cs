using System;
using Microsoft.AspNetCore.Mvc;
using BankDemo.DTOs;
using BankDemo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
namespace BankDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Registracija uspješna.");
        }

        [HttpPost("login")]
        [EnableRateLimiting("loginPolicy")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized("Pogrešno korisničko ime ili lozinka.");
            }

            return Ok(response);
        }
    


    [HttpPost("create-staff")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStaff(CreateStaffRequestDto request)
        {
            var result = await _authService.CreateStaffAsync(request);

            if (!result.Success)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("Nalog uspješno kreiran.");
        }
    }
}