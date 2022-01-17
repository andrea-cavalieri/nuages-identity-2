using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Nuages.Identity.Services.AspNetIdentity;
using Nuages.Sender.API.Sdk;

namespace Nuages.Identity.Services;

public class ForgotPasswordService : IForgotPasswordService
{
    private readonly NuagesUserManager _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMessageSender _messageSender;
    private readonly IWebHostEnvironment _env;

    public ForgotPasswordService(NuagesUserManager userManager, IHttpContextAccessor httpContextAccessor, IMessageSender messageSender, IWebHostEnvironment env)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
        _messageSender = messageSender;
        _env = env;
    }
    
    public async Task<ForgotPasswordResultModel> ForgotPassword(ForgotPasswordModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            return new ForgotPasswordResultModel
            {
                Success = true // Fake success
            };
        }

       
        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var scheme = _httpContextAccessor.HttpContext!.Request.Scheme;
        var host = _httpContextAccessor.HttpContext.Request.Host.Host;
        if (_env.IsDevelopment())
            host += ":" + _httpContextAccessor.HttpContext!.Request.Host.Port;
        
        var url =
            $"{scheme}://{host}/Account/ResetPassword?code={code}";

        await _messageSender.SendEmailUsingTemplateAsync(model.Email, "Password_Reset", new Dictionary<string, string>
        {
            { "Link", url }
        });

        await _httpContextAccessor.HttpContext.SignInAsync(NuagesIdentityConstants.ResetPasswordScheme, StorePasswordResetEmailInfo(user.Id, user.Email));

        
        return new ForgotPasswordResultModel
        {
            Success = true // Fake success
        };
    }
    
    private static ClaimsPrincipal StorePasswordResetEmailInfo(string userId, string email)
    {
        var identity = new ClaimsIdentity("PasswordReset");
        identity.AddClaim(new Claim(ClaimTypes.Name, userId));
        identity.AddClaim(new Claim(ClaimTypes.Email, email));
        return new ClaimsPrincipal(identity);
    }
}

public interface IForgotPasswordService
{
    Task<ForgotPasswordResultModel> ForgotPassword(ForgotPasswordModel model);
}

// ReSharper disable once ClassNeverInstantiated.Global
public class ForgotPasswordModel
{
    public string Email { get; set; } = string.Empty;
    public string? RecaptchaToken { get; set; }
}

public class ForgotPasswordResultModel
{
    public bool Success { get; set; }
}