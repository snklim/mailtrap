using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;

class MailtrapClient
{
    private readonly string _apiKey;
    private readonly string _apiBaseUrl;
    private readonly Logger _logger;

    public MailtrapClient(Logger logger, MailtrapConfiguration configuration)
    {
        _logger = logger;
        _apiBaseUrl = configuration.ApiBaseUrl;
        _apiKey = configuration.ApiKey;
    }
    
    public async Task SendAsync(Mail mail)
    {
        new MailValidator().Validate(mail);

        var json = new MailJsonBuilder().Build(mail);
        
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var result = await client.PostAsync($"{_apiBaseUrl}/api/send", new StringContent(json, Encoding.UTF8, "application/json"));

        result.EnsureSuccessStatusCode();

        var response = await result.Content.ReadAsStringAsync();

        _logger.LogInfo($"Mail sent: {response}");
    }
}

class MailValidator
{
    public void Validate(Mail mail)
    {
        if (mail.From == null || string.IsNullOrEmpty(mail.From.Email))
            throw new ArgumentException("From is required");
        if (mail.To == null || mail.To.Count == 0 || mail.To.Any(address => string.IsNullOrEmpty(address.Email)))
            throw new ArgumentException("To is required");
        if (string.IsNullOrEmpty(mail.Subject))
            throw new ArgumentException("Subject is required");
        if (string.IsNullOrEmpty(mail.Text) && string.IsNullOrEmpty(mail.Html))
            throw new ArgumentException("Text or Html is required");
        if (mail.Attachments != null && mail.Attachments.Any(attachment => attachment.Content == null || attachment.Content.Length == 0))
            throw new ArgumentException("Attachment content is required");
    }
}

class Logger
{
    public void LogError(string message)
    {
        Console.WriteLine($"ERROR: {message}");
    }

    public void LogInfo(string message)
    {
        Console.WriteLine($"INFO: {message}");
    }
}

class MailtrapConfiguration
{
    public string ApiBaseUrl { get; set; }
    public string ApiKey { get; set; }
}

class MailJsonBuilder
{
    public string Build(Mail mail)
    {
        var parts = new []
        {
            GetFromPart(mail),
            GetToPart(mail),
            GetSubjectPart(mail),
            GetTextPart(mail),
            GetHtmlPart(mail),
            GetCategoryPart(mail),
            GetAttachmentsPart(mail)
        }.Where(part => !string.IsNullOrEmpty(part));

        return $"{{ {string.Join(",", parts)} }}";
    }

    private string GetSubjectPart(Mail mail)
    {
        return $"\"subject\": \"{mail.Subject}\"";
    }

    private string GetTextPart(Mail mail)
    {
        if (string.IsNullOrEmpty(mail.Text))
            return string.Empty;
        return $"\"text\": \"{mail.Text}\"";
    }

    private string GetCategoryPart(Mail mail)
    {
        if (string.IsNullOrEmpty(mail.Category))
            return string.Empty;
        return $"\"category\": \"{mail.Category}\"";
    }

    public string GetAttachmentsPart(Mail mail)
    {
        if (mail.Attachments == null || mail.Attachments.Count == 0)
            return string.Empty;
        var attachments = mail.Attachments.Select(attachment => $"{{\"content\": \"{Convert.ToBase64String(attachment.Content)}\", \"filename\": \"{attachment.FileName}\"}}");
        return $"\"attachments\": [{string.Join(",", attachments)}]";
    }

    private string GetHtmlPart(Mail mail)
    {
        if (string.IsNullOrEmpty(mail.Html))
            return string.Empty;
        return $"\"html\": \"{mail.Html}\"";
    }

    private string GetFromPart(Mail mail)
    {
        return $"\"from\": {GetAddressPart(mail.From)}";
    }

    private string GetToPart(Mail mail)
    {
        return $"\"to\": [{string.Join(",", mail.To.Select(address => GetAddressPart(address)))}]";
    }

    private string GetAddressPart(Address address)
    {
        if (string.IsNullOrEmpty(address.Name))
            return $"{{\"email\": \"{address.Email}\"}}";
        return $"{{\"email\": \"{address.Email}\", \"name\": \"{address.Name}\"}}";
    }
}

class MailBuilder
{
    private readonly Mail _mail = new Mail();

    public MailBuilder WithFrom(string email, string name = null)
    {
        _mail.From = new Address
        {
            Email = email,
            Name = name
        };
        return this;
    }

    public MailBuilder WithTo(string email, string name = null)
    {
        _mail.To.Add(new Address
        {
            Email = email,
            Name = name
        });
        return this;
    }

    public MailBuilder WithSubject(string subject)
    {
        _mail.Subject = subject;
        return this;
    }

    public MailBuilder WithText(string text)
    {
        _mail.Text = text;
        return this;
    }

    public MailBuilder WithHtml(string html)
    {
        _mail.Html = html;
        return this;
    }

    public MailBuilder WithCategory(string category)
    {
        _mail.Category = category;
        return this;
    }

    public MailBuilder WithAttachment(byte[] content, string fileName)
    {
        _mail.Attachments.Add(new Attachment
        {
            Content = content,
            FileName = fileName
        });
        return this;
    }

    public Mail Build()
    {
        return _mail;
    }
}

class Mail
{
    public Address From { get; set; }
    public IList<Address> To { get; set; } = new List<Address>();
    public string Subject { get; set; }
    public string Text { get; set; }
    public string Html { get; set; }
    public string Category { get; set; }
    public IList<Attachment> Attachments { get; set; } = new List<Attachment>();
}

struct Attachment
{
    public byte[] Content { get; set; }
    public string FileName { get; set; }
}

class Address
{
    public string Email { get; set; }
    public string Name { get; set; }
}
