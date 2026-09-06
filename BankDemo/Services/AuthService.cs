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

        private readonly string? _backupAdminApiKey;



        public AuthService(IUserRepository userRepository, ITokenService tokenService, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _backupAdminApiKey = configuration["Stripe:ApiKey"];

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

            // VULN: Logovanje osjetljivih podataka (lozinke) u običnom tekstu
            Console.WriteLine($"Login pokusaj: {request.Username} / {request.Password}");

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return null;
            }

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

            return new RegisterResult { Success = true };
        }
    }
}