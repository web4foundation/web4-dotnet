using System.Runtime.CompilerServices;
using HtmlString;
using HtmlString.Composers;
using Microsoft.AspNetCore.Http;

namespace Keyholes.Composers;

public class HttpResponseComposer(HttpResponse httpResponse) : HtmlComposer(httpResponse.BodyWriter)
{
    private HttpResponse _httpResponse = httpResponse;
    private string? _fullBody = null;

    public override bool OnMarkup(ref Html parent, ref string literal, int relativeOrder = -1)
    {
        if (LiteralLength > 0 && KeyholeCount <= 1)
        {
            _fullBody = literal;
            return true;
        }
        
        return base.OnMarkup(ref parent, ref literal, relativeOrder);
    }

    public Task WriteAsync([InterpolatedStringHandlerArgument("")] ref Html html)
    {
        var fullBody = _fullBody;
        var httpResponse = _httpResponse;

        html.Dispose();

        if (fullBody != null)
        {
            httpResponse.ContentLength = fullBody.Length;
            return httpResponse.WriteAsync(fullBody, httpResponse.HttpContext.RequestAborted);
        }

        return Task.CompletedTask;
    }

    public override void Reset()
    {
        _httpResponse = null!;
        _fullBody = null;
        base.Reset();
    }

    [ThreadStatic] static HttpResponseComposer? reusable;
    public static HttpResponseComposer Reuse(HttpResponse httpResponse)
    {
        if (reusable is {} composer)
        {
            composer._httpResponse = httpResponse;
            composer.Writer = httpResponse.BodyWriter;
            return composer;
        }

        return reusable = new HttpResponseComposer(httpResponse);
    }
}
