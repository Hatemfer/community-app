using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace community_app.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var emailSettings = _config.GetSection("EmailSettings");
            var smtpUser = emailSettings["SmtpUser"];
            var smtpPass = emailSettings["SmtpPass"];
            var smtpHost = emailSettings["SmtpHost"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Community App", smtpUser));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var smtp = new SmtpClient();
            
            // Helpful for avoiding timeouts/blocks in some environments
            smtp.CheckCertificateRevocation = false;

            try
            {
                _logger.LogInformation("Connecting to SMTP server {SmtpHost}:{SmtpPort}...", smtpHost, smtpPort);
                await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.Auto);
                
                _logger.LogInformation("Authenticating as {SmtpUser}...", smtpUser);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                
                await smtp.SendAsync(email);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication failed for SMTP user {SmtpUser}. Check if App Password is correct.", smtpUser);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending email to {ToEmail}", toEmail);
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}