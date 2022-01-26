using Microsoft.AspNetCore.Identity;

namespace Nuages.Identity.Services;

public class NuagesIdentityOptions
{
    public string[]? Audiences { get; set; } 
    
    public PasswordOptions Password { get; set; } = new ();

    public bool SupportsStartEnd { get; set; }
    
    public bool EnableAutoPasswordExpiration { get; set; }
    public int AutoExpirePasswordDelayInDays { get; set; }

    public bool EnableUserLockout { get; set; } = true;

    public int RequireConfirmedEmailGracePeriodInMinutes { get; set; } = 60;
    public int RequireConfirmedPhoneGracePeriodInMinutes { get; set; } = 60;

    public bool SupportsUserName { get; set; } = true;
    public bool RequireConfirmedEmail { get; set; }

    public bool AutoConfirmExternalLogin { get; set; }

    public string Authority { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    

}