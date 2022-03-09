﻿using System.Text;
using Fido2NetLib;
using Microsoft.AspNetCore.Identity;
using Nuages.Fido2.Storage;

namespace Nuages.Fido2.AspNetIdentity;

public class Fido2UserStore<TUser, TKey> : IFido2UserStore 
                where TKey : IEquatable<TKey> 
                where TUser : IdentityUser<TKey>
{
    private readonly UserManager<TUser> _userManager;

    public Fido2UserStore(UserManager<TUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Fido2User?> GetUserAsync(string userName)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user != null)
        {
            return new Fido2User
            {
                Name = userName,
                DisplayName = userName,
                Id = Encoding.UTF8.GetBytes(user.Id.ToString()!)
            };
        }

        return null;
    }
}