using BankDemo.DTOs;
using BankDemo.Models;
using BankDemo.Enums;
using BankDemo.Interfaces;

namespace BankDemo.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<RegisterResult> RegisterAsync(RegisterRequestDto request)
        {
            bool exists = await _userRepository.ExistsByUsernameAsync(request.Username);
            if (exists)
            {
                return new RegisterResult { Success = false, ErrorMessage = "Korisničko ime je zauzeto." };
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Client
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return new RegisterResult { Success = true };
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Neuspješan pokušaj logina za korisničko ime {Username}", request.Username);
                return null;
            }

            _logger.LogInformation("Uspješan login korisnika {Username} (Id: {UserId}, Rola: {Role})",
                user.Username, user.Id, user.Role);

            var token = _tokenService.GenerateToken(user!);

            return new LoginResponseDto
            {
                Token = token,
                Username = user!.Username
            };
        }

        public async Task<RegisterResult> CreateStaffAsync(CreateStaffRequestDto request)
        {
            bool exists = await _userRepository.ExistsByUsernameAsync(request.Username);
            if (exists)
            {
                return new RegisterResult { Success = false, ErrorMessage = "Korisničko ime je zauzeto." };
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("Novi staff nalog kreiran: {Username}, Rola: {Role}", user.Username, user.Role);


            return new RegisterResult { Success = true };
        }
    }
}