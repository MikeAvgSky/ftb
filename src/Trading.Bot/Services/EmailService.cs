namespace Trading.Bot.Services;

public class EmailService
{
    private readonly EmailConfiguration _emailConfig;
    private readonly ILogger<EmailService> _logger;

    public EmailService(EmailConfiguration emailConfig, ILogger<EmailService> logger)
    {
        _emailConfig = emailConfig;
        _logger = logger;
    }

    public async Task SendMailAsync(EmailData emailData)
    {
        try
        {
            using var emailMessage = new MimeMessage();

            var emailFrom = new MailboxAddress(_emailConfig.UserName, _emailConfig.From);

            emailMessage.From.Add(emailFrom);

            var emailTo = new MailboxAddress(emailData.EmailToName, emailData.EmailToAddress);

            emailMessage.To.Add(emailTo);

            emailMessage.Subject = emailData.EmailSubject;

            var emailBodyBuilder = new BodyBuilder
            {
                TextBody = emailData.EmailBody
            };

            emailMessage.Body = emailBodyBuilder.ToMessageBody();

            using var mailClient = new SmtpClient();

            await mailClient.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.Port,
                MailKit.Security.SecureSocketOptions.StartTls);

            await mailClient.AuthenticateAsync(_emailConfig.UserName, _emailConfig.Password);

            await mailClient.SendAsync(emailMessage);

            await mailClient.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send an email.");
        }
    }
}