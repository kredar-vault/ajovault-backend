using AjoVault.API.Auth.Dto;

namespace AjoVault.API.Auth;

public class AuthService(UserRepository userRepo, JwtService jwtService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var existing = await userRepo.FindByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            AccountNumber = GenerateAccountNumber()
        };

        await userRepo.AddAsync(user);

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(user.Id, user.Email),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(user.Id, user.Email),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task ForgotPasswordAsync(string email)
    {
        // If email doesn't exist, we still return success to prevent email enumeration
        var user = await userRepo.FindByEmailAsync(email);
        if (user == null) return;

        // In production, send reset email via Resend here
        // For hackathon: no-op but endpoint is wired
    }

    private static string GenerateAccountNumber()
    {
        var random = new Random();
        return string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(10).ToString()));
    }
}
