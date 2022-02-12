using Amazon.XRay.Recorder.Core;
using Microsoft.AspNetCore.Mvc;
using Nuages.Identity.UI.OpenIdDict;

namespace Nuages.Identity.UI.Controllers;

[ApiController]
[Route("connect")]
public class AuthorizationController : Controller
{
    private readonly IAuthorizeEndpoint _authorizeEndpoint;
    private readonly ILogger<AuthorizationController> _logger;
    private readonly ILogoutEndpoint _logoutEndpoint;
    private readonly ITokenEndpoint _tokenEndpoint;

    public AuthorizationController(
        IAuthorizeEndpoint authorizeEndpoint,
        ILogoutEndpoint logoutEndpoint,
        ITokenEndpoint tokenEndpoint, ILogger<AuthorizationController> logger)
    {
        _authorizeEndpoint = authorizeEndpoint;
        _logoutEndpoint = logoutEndpoint;
        _tokenEndpoint = tokenEndpoint;
        _logger = logger;
    }

    [HttpPost("token")]
    [Produces("application/json")]
    public async Task<IActionResult> Token()
    {
        try
        {
            AWSXRayRecorder.Instance.BeginSubsegment("AuthorizationController.Token");

            return await _tokenEndpoint.Exchange();
        }
        catch (Exception e)
        {
            AWSXRayRecorder.Instance.AddException(e);
            _logger.LogError(e, e.Message);

            throw;
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }
    }

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        try
        {
            AWSXRayRecorder.Instance.BeginSubsegment("AuthorizationController.Authorize");

            return await _authorizeEndpoint.Authorize();
        }
        catch (Exception e)
        {
            AWSXRayRecorder.Instance.AddException(e);
            _logger.LogError(e, e.Message);

            throw;
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            AWSXRayRecorder.Instance.BeginSubsegment("AuthorizationController.Logout");

            return await _logoutEndpoint.Logout();
        }
        catch (Exception e)
        {
            AWSXRayRecorder.Instance.AddException(e);
            _logger.LogError(e, e.Message);

            throw;
        }
        finally
        {
            AWSXRayRecorder.Instance.EndSubsegment();
        }
    }
}