using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Nuages.Identity.Services.AspNetIdentity;
using Nuages.Sender.API.Sdk;

namespace Nuages.Identity.Services;

public class SendEmailConfirmationService : ISendEmailConfirmationService
{
    private readonly NuagesUserManager _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;
    private readonly IMessageSender _messageSender;

    public SendEmailConfirmationService(NuagesUserManager userManager, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env, IMessageSender messageSender)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _env = env;
        _messageSender = messageSender;
    }
    
    public async Task<SendEmailConfirmationResultModel> SendEmailConfirmation(SendEmailConfirmationModel model)
    {
        if (string.IsNullOrEmpty(model.Email))
            throw new ArgumentNullException(model.Email);

        var email = model.Email;
        
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return new SendEmailConfirmationResultModel
            {
                Success = true // Fake success
            };
        }
       
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var scheme = _httpContextAccessor.HttpContext!.Request.Scheme;
        var host = _httpContextAccessor.HttpContext.Request.Host.Host;
        if (_env.IsDevelopment())
            host += ":" + _httpContextAccessor.HttpContext!.Request.Host.Port;
        
        var url =
            $"{scheme}://{host}/Account/ConfirmEmail?code={code}&userId={user.Id}";
        
        await _messageSender.SendEmailUsingTemplateAsync(user.Email, "Confirm_Email", new Dictionary<string, string>
        {
            { "Link", url }
        });
        
        return new SendEmailConfirmationResultModel
        {
            Success = true // Fake success
        };
    }
}

public interface ISendEmailConfirmationService
{
    Task<SendEmailConfirmationResultModel> SendEmailConfirmation(SendEmailConfirmationModel model);
}

// ReSharper disable once ClassNeverInstantiated.Global
public class SendEmailConfirmationModel
{
    public string? RecaptchaToken { get; set; }
    public string? Email { get; set; }
}

public class SendEmailConfirmationResultModel
{
    public bool Success { get; set; }
}