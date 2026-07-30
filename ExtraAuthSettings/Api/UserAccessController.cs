using System;
using System.Linq;
using System.Threading.Tasks;
using ExtraAuthSettings.Services;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Mvc;

namespace ExtraAuthSettings.Api;

/// <summary>
/// Exposes managed-user access information for the plugin configuration page.
/// </summary>
[ApiController]
[Route("Plugins/ExtraAuthSettings/Users")]
public class UserAccessController : ControllerBase
{
    private readonly IAuthorizationContext _authorizationContext;
    private readonly IUserManager _userManager;
    private readonly UserAccessSettingsService _userAccessSettingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccessController"/> class.
    /// </summary>
    /// <param name="authorizationContext">The authorization context.</param>
    /// <param name="userManager">The user manager.</param>
    /// <param name="userAccessSettingsService">The user access settings service.</param>
    public UserAccessController(
        IAuthorizationContext authorizationContext,
        IUserManager userManager,
        UserAccessSettingsService userAccessSettingsService)
    {
        _authorizationContext = authorizationContext;
        _userManager = userManager;
        _userAccessSettingsService = userAccessSettingsService;
    }

    /// <summary>
    /// Gets the Jellyfin-managed users with the plugin's resolved restrictions.
    /// </summary>
    /// <returns>The managed users.</returns>
    [HttpGet]
    public async Task<ActionResult<UserAccessViewModel[]>> GetUsers()
    {
        var actor = (await _authorizationContext.GetAuthorizationInfo(HttpContext).ConfigureAwait(false)).User;
        if (actor is null || !actor.HasPermission(PermissionKind.IsAdministrator))
        {
            return Forbid();
        }

        return Ok(_userManager.GetUsers()
            .OrderBy(user => user.Username, StringComparer.OrdinalIgnoreCase)
            .Select(user => CreateViewModel(user))
            .ToArray());
    }

    private UserAccessViewModel CreateViewModel(User user)
    {
        return new UserAccessViewModel
        {
            Id = user.Id,
            Name = user.Username,
            IsAdministrator = user.HasPermission(PermissionKind.IsAdministrator),
            AllowPasswordChange = _userAccessSettingsService.CanChangePassword(user.Id),
            AllowProfileImageChange = _userAccessSettingsService.CanChangeProfileImage(user.Id)
        };
    }
}
