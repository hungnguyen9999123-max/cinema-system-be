using System.Net;

namespace CinemaSystem.Common.EmailTemplates;

public static class VerificationEmailTemplate
{
    public static string Build(string fullName, string verificationLink)
    {
        var safeName = WebUtility.HtmlEncode(fullName);
        var safeLink = WebUtility.HtmlEncode(verificationLink);

        return $"""
        <html>
          <body style="font-family: Arial, sans-serif; background-color: #f8f8f8; padding: 24px; color: #1f2937;">
            <div style="max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; padding: 32px; border: 1px solid #e5e7eb;">
              <h2 style="margin-top: 0;">Verify your email</h2>
              <p>Hello {safeName},</p>
              <p>Thanks for registering with Cinema System. Please verify your email address by clicking the button below.</p>
              <p style="margin: 32px 0;">
                <a href="{safeLink}" style="display: inline-block; background: #111827; color: #ffffff; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 600;">
                  Verify Email
                </a>
              </p>
              <p>If the button does not work, copy and paste this link into your browser:</p>
              <p style="word-break: break-all;">{safeLink}</p>
              <p style="margin-bottom: 0; color: #6b7280;">This verification link can only be used once and expires in 30 minutes.</p>
            </div>
          </body>
        </html>
        """;
    }
}
