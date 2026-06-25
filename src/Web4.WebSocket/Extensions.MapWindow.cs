using Keyholes.Composers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Keyholes;
using System.Diagnostics;
using System.IO.Pipelines;
using HtmlString;
using HtmlString.Composers;
using Keyholes.Assets;

namespace Web4.WebSocket;

public static partial class Extensions
{
    /// <summary>
    /// Adds a RouteEndpoint for the specified pattern that establishes a 
    /// Web4 connection enabling the handling of events and manipulation of the DOM.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="template">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="WindowBuilder"/> that can be used to listen to events or further customize the endpoint.</returns>
    public static WindowBuilder MapWindow(
        this WebApplication app,
        [StringSyntax("Route")] string pattern,
        Func<Html> template)
    {
        var applicationBuilder = app.UseWebSockets();
        var group = app.MapGroup(pattern);
        var window = new WindowBuilder(template);

        group.Map("/", async httpContext =>
        {
            var pipeWriter = httpContext.Response.BodyWriter;
            var composer = HtmlKeyComposer.Reuse(pipeWriter, window);
            await httpContext.WriteAsync(composer, window.Template);
        });

        group.Map("/app", async httpContext =>
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var logger = app.Services.GetRequiredService<ILogger<Bridge>>();
                await Bridge.Bind(
                    httpContext,
                    window,
                    logger,
                    app.Lifetime.ApplicationStopping
                );
            }
        });

        if (!applicationBuilder.Properties.TryGetValue("IS_WEB4_MAPPED", out var isWeb4Mapped))
        {
            applicationBuilder.Properties["IS_WEB4_MAPPED"] = true;

            app.Map("/_app/websocket/kernel", (HttpContext context) =>
                WriteAsset(context, "text/javascript", AssetsHelper.GetWeb4Js(context.Request.Headers.AcceptEncoding.ToString())));

            app.Map("/_app/base/ui", (HttpContext context) =>
                WriteAsset(context, "text/css", AssetsHelper.GetWeb4Css(context.Request.Headers.AcceptEncoding.ToString())));

            app.Map("/_app/alive", async httpContext =>
            {
                if (httpContext.WebSockets.IsWebSocketRequest)
                    await httpContext.WebSockets.AcceptWebSocketAsync();
            });
        }

        return window;
    }

    private static void WriteAsset(HttpContext context, string contentType, (byte[] Body, string? ContentEncoding) asset)
    {
        context.Response.ContentType = contentType;
        context.Response.Headers.Vary = "Accept-Encoding";
        if (asset.ContentEncoding is not null)
            context.Response.Headers.ContentEncoding = asset.ContentEncoding;
        context.Response.ContentLength = asset.Body.Length;
        context.Response.BodyWriter.Write(asset.Body);
    }

    private static ValueTask<FlushResult> WriteAsync<T>(
        this HttpContext httpContext,
        T composer,
        Func<Html> template,
        bool includeServerTiming = false) // TODO: Move `includeServerTiming` to Config
            where T : BaseComposer, IStreamingComposer
    {
        var pipeWriter = httpContext.Response.BodyWriter;
        if (!includeServerTiming)
        {
            pipeWriter.Write(composer, $"{template()}");
            return pipeWriter.FlushAsync(httpContext.RequestAborted);
        }
        else
        {
            long gc1 = GC.GetAllocatedBytesForCurrentThread();
            long stopwatch = Stopwatch.GetTimestamp();

            pipeWriter.Write(composer, $"{template()}");

            var elapsed = Stopwatch.GetElapsedTime(stopwatch);
            long gc2 = GC.GetAllocatedBytesForCurrentThread();

            // This allocates.  Boo!  But it occurs after measurement.
            httpContext.Response.Headers["Server-Timing"] = $"""
                allocations;desc="Allocations: {gc2 - gc1}b", render;desc="Web4.Render";dur={elapsed.TotalNanoseconds / 1_000_000d}
                """;

            return pipeWriter.FlushAsync(httpContext.RequestAborted);
        }
    }
}