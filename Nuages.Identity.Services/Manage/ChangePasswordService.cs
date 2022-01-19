using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Nuages.Identity.Services.AspNetIdentity;
using Nuages.Web.Exceptions;

// ReSharper disable ClassNeverInstantiated.Global

namespace Nuages.Identity.Services.Manage;

public class ChangePasswordService : IChangePasswordService
{
    private readonly NuagesUserManager _userManager;
    private readonly NuagesSignInManager _signInManager;
    private readonly IStringLocalizer _localizer;

    public ChangePasswordService(NuagesUserManager userManager, NuagesSignInManager signInManager, IStringLocalizer localizer)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _localizer = localizer;
    }
    
    public async Task<ChangePasswordResultModel> ChangePasswordAsync(string userId, ChangePasswordModel model)
    {
        ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(userId);
        ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(model.CurrentPassword);
        ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(model.NewPassword);
        ArgumentNullOrEmptyException.ThrowIfNullOrEmpty(model.NewPasswordConfirm);

        if (model.NewPassword != model.NewPasswordConfirm)
        {
            return new ChangePasswordResultModel
            {
                Errors = new List<string>
                {
                    _localizer["resetPassword.passwordConfirmDoesNotMatch"]
                }
            };
        }
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new NotFoundException("UserNotFound");

        IdentityResult res;

        if (await _userManager.HasPasswordAsync(user))
        {
            res = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        }
        else
        {
            res = await _userManager.AddPasswordAsync(user, model.NewPassword);
        }
        
        if (res.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
        }
        
        return new ChangePasswordResultModel
        {
            Success = res.Succeeded,
            Errors = res.Errors.Select(e => _localizer[$"identity.{e.Code}"].Value).ToList()
        };
    }
}

public interface IChangePasswordService
{
    Task<ChangePasswordResultModel> ChangePasswordAsync(string userid, ChangePasswordModel model);
}

public class ChangePasswordModel
{
    
    public string NewPassword { get; set; } = string.Empty;
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPasswordConfirm { get; set; } = string.Empty;
}

public class ChangePasswordResultModel
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
}