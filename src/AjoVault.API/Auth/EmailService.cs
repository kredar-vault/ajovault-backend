using AjoVault.API.Config;
using Microsoft.Extensions.Options;
using Resend;

namespace AjoVault.API.Auth;

public class EmailService(IResend resend, IOptions<EmailSettings> emailOptions, ILogger<EmailService> logger)
{
    private readonly EmailSettings _email = emailOptions.Value;

    public Task SendLoginOtpEmailAsync(string toEmail, string fullName, string otp) =>
        SendOtpEmailAsync(toEmail, fullName, otp, subject: "Your AjoVault login code");

    public async Task SendResetPasswordEmailAsync(string toEmail, string fullName, string resetLink)
    {
        var message = new EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromEmail}>",
            To = [toEmail],
            Subject = "Reset your AjoVault password",
            HtmlBody = $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f4;padding:40px 0;">
                    <tr><td align="center">
                      <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">
                        <tr>
                          <td align="center" style="padding:0 0 24px 0;">
                            <span style="font-size:22px;font-weight:800;color:#0f5132;letter-spacing:-0.5px;">Ajo<span style="color:#198754;">Vault</span></span>
                          </td>
                        </tr>
                        <tr>
                          <td style="background:#ffffff;border-radius:12px;padding:48px 48px 40px;box-shadow:0 1px 4px rgba(0,0,0,0.08);">
                            <p style="margin:0 0 8px;font-size:22px;font-weight:700;color:#0f2417;">Reset your password</p>
                            <p style="margin:0 0 32px;font-size:15px;color:#4b5563;line-height:1.6;">
                              Hi {fullName}, click the button below to reset your AjoVault password. This link expires in <strong>10 minutes</strong>.
                            </p>
                            <div style="text-align:center;margin-bottom:32px;">
                              <a href="{resetLink}" style="display:inline-block;background:#006C49;color:#ffffff;font-size:15px;font-weight:700;padding:14px 36px;border-radius:10px;text-decoration:none;letter-spacing:-0.2px;">Reset Password</a>
                            </div>
                            <p style="margin:0;font-size:13px;color:#9ca3af;line-height:1.6;">
                              If you did not request a password reset, you can safely ignore this email. Your password will not change.
                            </p>
                          </td>
                        </tr>
                        <tr>
                          <td align="center" style="padding:24px 0 0;">
                            <p style="margin:0;font-size:12px;color:#9ca3af;">&copy; 2026 AjoVault. Powered by Kredar.</p>
                          </td>
                        </tr>
                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
            """
        };

        if (string.IsNullOrEmpty(_email.ApiKey))
        {
            logger.LogWarning("[DEV] Reset password link for {Email}: {Link}", toEmail, resetLink);
            return;
        }

        await resend.EmailSendAsync(message);
    }

    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otp, string subject = "Your AjoVault verification code")
    {
        var message = new EmailMessage
        {
            From = $"{_email.FromName} <{_email.FromEmail}>",
            To = [toEmail],
            Subject = subject,
            HtmlBody = $"""
                <!DOCTYPE html>
                <html>
                <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
                <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f4f4;padding:40px 0;">
                    <tr><td align="center">
                      <table width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;">

                        <tr>
                          <td align="center" style="padding:0 0 24px 0;">
                            <span style="font-size:22px;font-weight:800;color:#0f5132;letter-spacing:-0.5px;">Ajo<span style="color:#198754;">Vault</span></span>
                          </td>
                        </tr>

                        <tr>
                          <td style="background:#ffffff;border-radius:12px;padding:48px 48px 40px;box-shadow:0 1px 4px rgba(0,0,0,0.08);">
                            <p style="margin:0 0 8px;font-size:22px;font-weight:700;color:#0f2417;">Verify your email</p>
                            <p style="margin:0 0 32px;font-size:15px;color:#4b5563;line-height:1.6;">
                              Hi {fullName}, enter the code below to complete your AjoVault registration.
                            </p>

                            <div style="background:#f0fdf4;border:2px solid #86efac;border-radius:12px;padding:28px;text-align:center;margin-bottom:32px;">
                              <span style="font-size:42px;font-weight:800;letter-spacing:14px;color:#15803d;font-family:monospace;">{otp}</span>
                            </div>

                            <p style="margin:0;font-size:13px;color:#9ca3af;line-height:1.6;">
                              This code expires in <strong>10 minutes</strong>. If you did not create an AjoVault account, you can safely ignore this email.
                            </p>
                          </td>
                        </tr>

                        <tr>
                          <td align="center" style="padding:24px 0 0;">
                            <p style="margin:0;font-size:12px;color:#9ca3af;">&copy; 2026 AjoVault. Powered by Kredar.</p>
                          </td>
                        </tr>

                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
            """
        };

        if (string.IsNullOrEmpty(_email.ApiKey))
        {
            logger.LogWarning("[DEV] EmailSettings:ApiKey not set — OTP for {Email}: {Otp}", toEmail, otp);
            return;
        }

        await resend.EmailSendAsync(message);
    }
}
