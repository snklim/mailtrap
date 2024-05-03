using System.Net;
using System.Net.Mail;

var logger = new Logger();
var configuration = new MailtrapConfiguration
{
    ApiBaseUrl = "https://send.api.mailtrap.io",
    ApiKey = "c530b034930710bae1fe17017e4008d7"
};
var mail = new MailBuilder()
    .WithFrom("mailtrap@demomailtrap.com", "Mailtrap Test")
    .WithTo("sergey.klimenko@gmail.com")
    .WithSubject("You are awesome!")
    .WithText("Congrats for sending test email with Mailtrap!")
    .WithCategory("Integration Test")
    .WithAttachment(System.IO.File.ReadAllBytes("welcome.png"), "welcome.png")
    .WithAttachment(System.IO.File.ReadAllBytes("welcome.png"), "welcome2.png")
    .Build();

try
{
    await new MailtrapClient(logger, configuration).SendAsync(mail);
}
catch (Exception ex)
{
    logger.LogError(ex.Message);
}