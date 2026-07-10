using AjoVault.API.Auth.Dto;

namespace AjoVault.API.Auth;

public class AuthService(UserRepository userRepo, JwtService jwtService, EmailService emailService)
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var existing = await userRepo.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            if (!existing.IsVerified)
            {
                // Resend OTP to existing unverified account
                await SendOtpAsync(existing);
                return new RegisterResponse { UserId = existing.Id, Email = existing.Email, Message = "A new verification code has been sent to your email." };
            }
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10),
            AccountNumber = GenerateAccountNumber(),
            IsVerified = false,
        };

        await userRepo.AddAsync(user);
        await SendOtpAsync(user);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Message = "Registration successful. Enter the OTP sent to your email to continue."
        };
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("No account found with that email.");

        if (user.IsVerified)
            throw new InvalidOperationException("This account is already verified. Please log in.");

        if (user.OtpCode == null || user.OtpExpiresAt == null)
            throw new InvalidOperationException("No OTP found. Please request a new one.");

        if (DateTime.UtcNow > user.OtpExpiresAt)
            throw new InvalidOperationException("OTP has expired. Please request a new one.");

        if (user.OtpCode != request.Otp.Trim())
            throw new InvalidOperationException("Invalid OTP. Please try again.");

        user.IsVerified = true;
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        await userRepo.UpdateAsync(user);

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(user.Id, user.Email),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task ResendOtpAsync(string email)
    {
        var user = await userRepo.FindByEmailAsync(email)
            ?? throw new KeyNotFoundException("No account found with that email.");

        if (user.IsVerified)
            throw new InvalidOperationException("This account is already verified. Please log in.");

        await SendOtpAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsVerified)
            throw new InvalidOperationException("Please verify your email before logging in.");

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
        var user = await userRepo.FindByEmailAsync(email);
        if (user == null) return;
        // TODO: send reset email with token via Resend
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("No account found with that email.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 10);
        await userRepo.UpdateAsync(user);
    }

    private async Task SendOtpAsync(User user)
    {
        var otp = GenerateOtp();
        user.OtpCode = otp;
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await userRepo.UpdateAsync(user);
        await emailService.SendOtpEmailAsync(user.Email, user.FullName, otp);
    }

    private static string GenerateOtp() =>
        Random.Shared.Next(100_000, 999_999).ToString();

    private static string GenerateAccountNumber()
    {
        var random = new Random();
        return string.Concat(Enumerable.Range(0, 10).Select(_ => random.Next(10).ToString()));
    }
}
