// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Nuages.Identity.Services.AspNetIdentity;

// ReSharper disable UnusedMember.Global

namespace Nuages.Identity.UI.Pages.Account.Manage;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly NuagesUserManager _userManager;

    public ChangePasswordModel(NuagesUserManager userManager)
    {
        _userManager = userManager;
    }
    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        if (!hasPassword)
        {
            return RedirectToPage("./SetPassword");
        }

        return Page();
    }
    
    // public async Task<IActionResult> OnPostAsync()
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return Page();
    //     }
    //
    //     var user = await _userManager.GetUserAsync(User);
    //     if (user == null)
    //     {
    //         return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
    //     }
    //
    //     var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
    //     if (!changePasswordResult.Succeeded)
    //     {
    //         foreach (var error in changePasswordResult.Errors)
    //         {
    //             ModelState.AddModelError(string.Empty, error.Description);
    //         }
    //         return Page();
    //     }
    //
    //     await _signInManager.RefreshSignInAsync(user);
    //     _logger.LogInformation("User changed their password successfully.");
    //     StatusMessage = "Your password has been changed.";
    //
    //     return RedirectToPage();
    // }
}