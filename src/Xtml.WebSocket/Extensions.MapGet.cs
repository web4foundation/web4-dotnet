using Microsoft.AspNetCore.Builder;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;
using HtmlString;
using Keyholes.Composers;

namespace Web4.WebSocket;

public static partial class Extensions
{
    public static IEndpointConventionBuilder MapGet(
        this IEndpointRouteBuilder endpoints,
        [StringSyntax("Route")] string pattern,
        Func<Html> template)
    {
        return endpoints.Map(pattern, async httpContext =>
        {
            // httpContext.Response.BodyWriter.Write($"{template()}");

            await HttpResponseComposer
                .Reuse(httpContext.Response)
                .WriteAsync($"{template()}");
        });
    }
}