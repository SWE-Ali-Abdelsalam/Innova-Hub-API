using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace InnoHub.Service.EmailSenderService
{
    public class EmailSender : IEmailSender
    {
        private readonly string _email;
        private readonly string _appPassword;

        public EmailSender(IConfiguration configuration)
        {
            // Retrieve email and password from configuration
            _email = configuration["EmailSender:Email"]
                     ?? throw new ArgumentNullException(nameof(_email), "Email configuration is missing.");
            _appPassword = configuration["EmailSender:AppPassword"]
                           ?? throw new ArgumentNullException(nameof(_appPassword), "AppPassword configuration is missing.");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Recipient email is required.", nameof(email));

            if (string.IsNullOrWhiteSpace(subject))
                throw new ArgumentException("Email subject is required.", nameof(subject));

            if (string.IsNullOrWhiteSpace(htmlMessage))
                throw new ArgumentException("Email message content is required.", nameof(htmlMessage));

            try
            {
                // Prepare the email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_email, "InnoHub"), // Add your "From" name here
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                // Configure the SMTP client
                using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential(_email, _appPassword),
                    EnableSsl = true
                };

                // Send the email
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log exception or rethrow for higher-level handling
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
