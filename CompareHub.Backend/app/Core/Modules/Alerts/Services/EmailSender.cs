using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using CompareHub.Backend.app.Core.Modules.Alerts.Interfaces;

namespace CompareHub.Backend.app.Core.Modules.Alerts.Services;

public class EmailSender : IEmailSender
{
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _user;
    private readonly string? _password;
    private readonly string _fromName;

    public EmailSender(IConfiguration configuration)
    {
        _host = configuration["Email:Host"] ?? "smtp.gmail.com";
        _port = int.TryParse(configuration["Email:Port"], out var port) ? port : 465;
        _user = configuration["Email:User"];
        _password = configuration["Email:Password"];
        _fromName = configuration["Email:FromName"] ?? "CompareHub";
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_user) && !string.IsNullOrWhiteSpace(_password);

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _user));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient
        {
            // Certificate chain and hostname are still validated; only the live revocation lookup is skipped,
            // because the OCSP/CRL servers aren't always reachable from hosting networks and that blocked all mail.
            CheckCertificateRevocation = false
        };
        await client.ConnectAsync(_host, _port, SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(_user, _password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
