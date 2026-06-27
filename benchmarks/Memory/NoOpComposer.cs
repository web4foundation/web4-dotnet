using System.Runtime.CompilerServices;
using HtmlString;
using HtmlString.Composers;

namespace Xtml.Composers;

public sealed class NoOpComposer : BaseComposer
{

    public void Compose([InterpolatedStringHandlerArgument("")] Html html)
    {
        // ^ That's the root Html getting passed in above.
        // By the time you've reached this line, the templating work has already completed.

        // Calls composer.Reset()
        html.Dispose();
    }
}