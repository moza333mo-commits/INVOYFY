using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;

namespace InvoifyWeb;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/404.html");
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseResponseCompression();

        // Security Headers Middleware
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "font-src 'self' https://fonts.gstatic.com data:; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            await next();
        });

        app.UseStatusCodePagesWithReExecute("/404.html");

        // Serve wwwroot/index.html directly as the single source of truth
        app.UseDefaultFiles();

        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".svg"] = "image/svg+xml";

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypeProvider,
            OnPrepareResponse = ctx =>
            {
                var fileName = ctx.File.Name;

                // Always revalidate HTML files so mobile browsers never get stuck on cached HTML
                if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                }
                else if (!app.Environment.IsDevelopment() &&
                        (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                         fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)))
                {
                    // Static assets use ?v=20261027 query string for instant cache-busting when updated
                    ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=86400";
                }
            }
        });

        // Open browser automatically in Development environment only
        if (app.Environment.IsDevelopment())
        {
            app.Lifetime.ApplicationStarted.Register(() =>
            {
                try
                {
                    var url = app.Urls.FirstOrDefault() ?? "http://localhost:5050";
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Ignore browser launch errors in headless environments
                }
            });
        }

        app.Run();
    }
}