using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Nuages.Identity.Services.AspNetIdentity;
using Nuages.Identity.Services.Email.Sender;
using Nuages.Web;
using Nuages.Web.Exceptions;

namespace Nuages.Identity.Services.Login.MagicLink;

public class MagicLinkService : IMagicLinkService
{
    private readonly IStringLocalizer _localizer;
    private readonly IMessageService _messageService;
    private readonly NuagesIdentityOptions _options;
    private readonly IRuntimeConfiguration _runtimeConfiguration;
    private readonly NuagesSignInManager _signinManager;
    private readonly NuagesUserManager _userManager;

    public MagicLinkService(NuagesUserManager userManager, NuagesSignInManager signinManager,
        IMessageService messageService, IStringLocalizer localizer,
        IOptions<NuagesIdentityOptions> options, IRuntimeConfiguration runtimeConfiguration)
    {
        _userManager = userManager;
        _signinManager = signinManager;
        _messageService = messageService;
        _localizer = localizer;
        _runtimeConfiguration = runtimeConfiguration;
        _options = options.Value;
    }

    public async Task<string> GetMagicLinkUrl(string userId, string? returnUrl = null)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("UserNotFound");

        return await GetMagicLinkUrl(user, returnUrl);
    }

    public async Task<MagicLinkResultModel> LoginMagicLink(string token, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        var isValid =
            await _userManager.VerifyUserTokenAsync(user, "MagicLinkLoginProvider", "magiclink-auth", token);
        if (!isValid)
            return new MagicLinkResultModel
            {
                Success = false
            };

        var result = await _signinManager.CustomPreSignInCheck(user);
        if (result is { Succeeded: false })
            return new MagicLinkResultModel
            {
                Result = result,
                Reason = user.LastFailedLoginReason,
                Message = _localizer[LoginService.GetMessageKey(user.LastFailedLoginReason)],
                Success = false
            };

        await UpdateSecurityStampAsync(user);

        result = await _signinManager.CustomSignInOrTwoFactorAsync(user, false);

        return new MagicLinkResultModel
        {
            Success = result.Succeeded,
            Result = result
        };
    }

    public async Task<StartMagicLinkResultModel> StartMagicLink(StartMagicLinkModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return new StartMagicLinkResultModel
            {
                Success = true //Fake success
            };

        var result = await _signinManager.CustomPreSignInCheck(user);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (result is { Succeeded: false })
            return new StartMagicLinkResultModel
            {
                Result = result,
                Reason = user.LastFailedLoginReason,
                Message = _localizer[LoginService.GetMessageKey(user.LastFailedLoginReason)],
                Success = false
            };

        var url = await GetMagicLinkUrl(user, model.ReturnUrl);

        _messageService.SendEmailUsingTemplate(user.Email, "MagicLink_Login", new Dictionary<string, string>
        {
            { "Link", url }
        });

        return new StartMagicLinkResultModel
        {
            Url = _runtimeConfiguration.IsTest ? url : null,
            Success = true
        };
    }

    private async Task<string> GetMagicLinkUrl(NuagesApplicationUser<string> user, string? returnUrl)
    {
        var token = await _userManager.GenerateUserTokenAsync(user, "MagicLinkLoginProvider",
            "magiclink-auth");

        var baseUrl = _options.Authority;

        return $"{baseUrl}/account/magicLinkLogin?token={token}&userId={user.Id}&returnUrl={WebUtility.UrlEncode(returnUrl)}";
    }


    private async Task UpdateSecurityStampAsync(NuagesApplicationUser<string> user)
    {
        if (_userManager.SupportsUserSecurityStamp)
            await _userManager.UpdateSecurityStampAsync(user);
    }
}

public class MagicLinkResultModel
{
    public bool Success { get; set; }
    public SignInResult Result { get; set; } = null!;
    public FailedLoginReason? Reason { get; set; }
    public string? Message { get; set; }
}

public interface IMagicLinkService
{
    //Task<string> GetPasswordlessUrl(string userId);
    Task<MagicLinkResultModel> LoginMagicLink(string token, string userId);
    Task<StartMagicLinkResultModel> StartMagicLink(StartMagicLinkModel model);
}

public class StartMagicLinkModel
{
    public string Email { get; set; } = string.Empty;
    public string ReturnUrl { get; set; }  = string.Empty;
}

public class StartMagicLinkResultModel
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public SignInResult Result { get; set; } = null!;
    public FailedLoginReason? Reason { get; set; }


    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

    public string? Url { get; set; }
}