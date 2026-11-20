using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IgelPortal.Web.Services;

public sealed class RecentAuthenticationFilter(bool requireOnGet = false) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        // External login linking completes in a GET callback. Authenticator secrets are also displayed on GET.
        var requiresConfirmation = requireOnGet || HttpMethods.IsPost(request.Method) ||
            context.HandlerMethod?.Name == "LinkLoginCallback";
        if (requiresConfirmation && !await PortalSignInManager.HasRecentAuthenticationAsync(context.HttpContext))
        {
            context.Result = new RedirectToPageResult("/Account/Login", new
            {
                area = "Identity",
                returnUrl = request.PathBase.Add(request.Path).Value,
                confirmAccount = true
            });
            return;
        }

        await next();
    }
}
