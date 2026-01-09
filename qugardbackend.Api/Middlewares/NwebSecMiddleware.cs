using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace examportal.Api.Middleware;

public static class NwebSecMiddleware
{
    public static IApplicationBuilder UseNWebSecurity(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.GetTypedHeaders().CacheControl =
             new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
             {
                 NoStore = true,
                 NoCache = true,
                 MustRevalidate = true,
                 MaxAge = TimeSpan.FromSeconds(0),
                 Private = true,
             };
            //context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000;includeSubDomains;preload");
            context.Response.Headers.Add("Pragma", "no-cache");
            context.Response.Headers.Add("X-Frame-Options", "DENY");

            if (context.Response.Headers.ContainsKey("Server"))
            {
                context.Response.Headers.Remove("Server");
            }

            if (context.Response.Headers.ContainsKey("x-powered-by") || context.Response.Headers.ContainsKey("X-Powered-By"))
            {
                context.Response.Headers.Remove("x-powered-by");
                context.Response.Headers.Remove("X-Powered-By");
            }

            await next();
        });

        app.UseXContentTypeOptions();
        app.UseReferrerPolicy(opts => opts.NoReferrer());
        app.UseXXssProtection(options => options.EnabledWithBlockMode());
        app.UseCsp(opts => opts
           .BlockAllMixedContent()
           .ScriptSources(s => s.Self())
           .StyleSources(s => s.Self()));

        return app;
    }
}