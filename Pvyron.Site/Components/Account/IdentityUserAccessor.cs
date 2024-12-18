using Microsoft.AspNetCore.Identity;
using Pvyron.Site.Data;

namespace Pvyron.Site.Components.Account;

internal sealed class IdentityUserAccessor(
    UserManager<WalibaUser> userManager,
    IdentityRedirectManager redirectManager)
{
    public async Task<WalibaUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser",
                $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}