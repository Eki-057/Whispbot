using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using static System.Net.Http.HttpMethod;
using System.Diagnostics.CodeAnalysis;
using YellowMacaroni.Discord.Core;
using YellowMacaroni.Discord.Cache;
using Newtonsoft.Json;

namespace Whispbot.API
{
    public class WhispbotAPI
    {
        public static void Start()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            string port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

            if (!Config.IsDev) builder.Logging.ClearProviders();

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(int.Parse(port));
            });

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.IncludeFields = true;
            });

            WebApplication app = builder.Build();

            foreach (var route in routes)
            {
                foreach (var method in route.Value)
                {
                    if (method.Key == Get)
                    {
                        app.MapGet($"/api{route.Key}", method.Value);
                    }
                    else if (method.Key == Post)
                    {
                        app.MapPost($"/api{route.Key}", method.Value);
                    }
                }
            }

            app.Run();
        }

        [StringSyntax("Route")]
        public static Dictionary<string, Dictionary<HttpMethod, RequestDelegate>> routes = new() {
            { "/ping", new() {
                { Get, async context => {
                        await context.Response.WriteAsJsonAsync(new {status = "ok"});
                    }
                }
            } },
            { "/mutuals", new() {
                { Post, async context => {
                    MutualsFormat? data = await context.Request.ReadFromJsonAsync<MutualsFormat>();

                    if (data is null)
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsJsonAsync(new { status = 400, message = "Failed to parse body" });
                        return;
                    }

                    List<object> response = [];

                    foreach (string id in data.guildIds)
                    {
                        Guild? guild = DiscordCache.Guilds.FromCache(id);
                        if (guild is null) continue;

                        response.Add(new { guild.id, (await guild.members.Get(data.userId))?.roles });
                    }

                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsJsonAsync(response);
                } }
            } }
        };
    }

    internal class MutualsFormat { public string userId = ""; public List<string> guildIds = []; }
}
