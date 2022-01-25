using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Nuages.Identity.Services.AspNetIdentity;
using Nuages.Identity.Services.Email;
using Nuages.Web.Exceptions;

// ReSharper disable ClassNeverInstantiated.Global

namespace Nuages.Identity.Services.Manage;

public class ChangePhoneNumberService : IChangePhoneNumberService
{
    private readonly NuagesUserManager _userManager;
    private readonly IStringLocalizer _localizer;
    private readonly IMessageService _messageService;

    public ChangePhoneNumberService(NuagesUserManager userManager, IStringLocalizer localizer, IMessageService messageService)
    {
        _userManager = userManager;
        _localizer = localizer;
        _messageService = messageService;
    }
    
    public async Task<ChangePhoneNumberResultModel> ChangePhoneNumberAsync(string userId, string phoneNumber, string? token)
    {
        ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(userId);
       
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("UserNotFound");

        var previousPhoneNumber = user.PhoneNumber;
        
        IdentityResult res;
        
        phoneNumber = phoneNumber.Replace("+", "").Replace("+", " ").Replace("+", "-");
        
        if (string.IsNullOrEmpty(token))
        {
            res = await _userManager.SetPhoneNumberAsync(user, phoneNumber);
        }
        else
        {
            ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(phoneNumber);
            res = await _userManager.ChangePhoneNumberAsync(user, phoneNumber, token);
        }

        if (res.Succeeded)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                await _messageService.SendEmailUsingTemplateAsync(user.Email, "Fallback_Phone_Removed", new Dictionary<string, string>
                {
                    { "PhoneNumber", previousPhoneNumber }
                });
            }
            else
            {
                await _messageService.SendEmailUsingTemplateAsync(user.Email, "Fallback_Phone_Added", new Dictionary<string, string>
                {
                    { "PhoneNumber", phoneNumber }
                });
            }
        }
        
        return new ChangePhoneNumberResultModel
        {
            Success = res.Succeeded,
            Errors = res.Errors.Select(e => _localizer[$"identity.{e.Code}"].Value).ToList()
        };
    }
}

public interface IChangePhoneNumberService
{
    Task<ChangePhoneNumberResultModel> ChangePhoneNumberAsync(string userId,string phone, string? token);
}

public class ChangePhoneNumberResultModel
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ChangePhoneNumberModel
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}