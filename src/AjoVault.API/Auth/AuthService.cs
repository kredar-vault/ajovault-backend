using AjoVault.API.Auth.Dto;
using AjoVault.API.Kredar;

namespace AjoVault.API.Auth;

public class AuthService(UserRepository userRepo, JwtService jwtService, EmailService emailService, ILogger<AuthService> logger, IServiceScopeFactory scopeFactory)
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

        // Provision personal DVA in background — don't block the response
        _ = Task.Run(() => ProvisionUserDvaAsync(user.Id));

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

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsVerified)
            throw new InvalidOperationException("Please verify your email before logging in.");

        await SendLoginOtpAsync(user);

        return new LoginResponse
        {
            Email = user.Email,
            Message = "A verification code has been sent to your email."
        };
    }

    public async Task<AuthResponse> VerifyLoginOtpAsync(VerifyLoginOtpRequest request)
    {
        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsVerified)
            throw new InvalidOperationException("Please verify your email before logging in.");

        if (user.OtpCode == null || user.OtpExpiresAt == null)
            throw new InvalidOperationException("No OTP found. Please log in again.");

        if (DateTime.UtcNow > user.OtpExpiresAt)
            throw new InvalidOperationException("OTP has expired. Please log in again.");

        if (user.OtpCode != request.Otp.Trim())
            throw new InvalidOperationException("Invalid OTP. Please try again.");

        user.OtpCode = null;
        user.OtpExpiresAt = null;
        await userRepo.UpdateAsync(user);

        // Re-provision DVA if it failed during signup
        if (user.DvaAccountNumber == null)
            _ = Task.Run(() => ProvisionUserDvaAsync(user.Id));

        return new AuthResponse
        {
            Token = jwtService.GenerateToken(user.Id, user.Email),
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email
        };
    }

    public async Task ResendLoginOtpAsync(string email)
    {
        var user = await userRepo.FindByEmailAsync(email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsVerified)
            throw new InvalidOperationException("Please verify your email before logging in.");

        await SendLoginOtpAsync(user);
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await userRepo.FindByEmailAsync(email);
        if (user == null) return; // don't reveal whether email exists

        var token = GenerateOtp();
        user.OtpCode = token;
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await userRepo.UpdateAsync(user);

        var resetLink = $"https://vault.staging.kredar.xyz/reset-password?email={Uri.EscapeDataString(email)}&token={token}";
        await emailService.SendResetPasswordEmailAsync(user.Email, user.FullName, resetLink);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var user = await userRepo.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("No account found with that email.");

        if (user.OtpCode != request.Token)
            throw new InvalidOperationException("Invalid or expired reset link. Please request a new one.");

        if (user.OtpExpiresAt == null || user.OtpExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Reset link has expired. Please request a new one.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 10);
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        await userRepo.UpdateAsync(user);
    }

    public async Task ProvisionUserDvaAsync(Guid userId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<UserRepository>();
            var kredar = scope.ServiceProvider.GetRequiredService<KredarClient>();

            var user = await repo.FindByIdAsync(userId);
            if (user == null || user.DvaAccountNumber != null) return;

            var nameParts = user.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 && !string.IsNullOrWhiteSpace(nameParts[1]) ? nameParts[1].Trim() : "User";
            var customer = await kredar.CreateOrGetCustomerAsync(firstName, lastName, user.Email, user.PhoneNumber);
            if (customer == null) return;

            var dva = await kredar.CreateOrGetDvaAsync(customer.Id, null);
            if (dva == null) return;

            user.KredarCustomerId = customer.Id;
            user.DvaAccountNumber = dva.AccountNumber;
            user.DvaBankName = dva.BankName;
            user.DvaAccountName = dva.AccountName;
            await repo.UpdateAsync(user);

            logger.LogInformation("Personal DVA {AccountNumber} provisioned for user {UserId}", dva.AccountNumber, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision personal DVA for user {UserId}", userId);
        }
    }

    private async Task SendLoginOtpAsync(User user)
    {
        var otp = GenerateOtp();
        user.OtpCode = otp;
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await userRepo.UpdateAsync(user);
        await emailService.SendLoginOtpEmailAsync(user.Email, user.FullName, otp);
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
