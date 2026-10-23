/*
===========================================================================
Copyright (C) 2015-2019 Project Meteor Dev Team

This file is part of Project Meteor Server.

Project Meteor Server is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Project Meteor Server is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with Project Meteor Server. If not, see <https:www.gnu.org/licenses/>.
===========================================================================
*/

using System;
using System.IO;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Meteor.Web.Data;
using Meteor.Web.Patching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using NLog;

namespace Meteor.Web
{
    // The web side of the game: ffxivboot's version check, ffxivlogin's login page, and sign-up.
    class Program
    {
        public static Logger Log;

        static void Main(string[] args)
        {
            Log = LogManager.GetCurrentClassLogger();
            Log.Info("==================================");
            Log.Info("Project Meteor: Web Server");
            Log.Info("Version: 0.1");
            Log.Info("==================================");

            ConfigConstants.Load();
            ConfigConstants.ApplyLaunchArgs(args);
            PatchCatalog.Load(Path.Combine(AppContext.BaseDirectory, "web_patches.json"),
                ConfigConstants.PATCH_BOOT_VERSION, ConfigConstants.PATCH_GAME_VERSION);
            PatchStore.Load(ConfigConstants.PATCH_DIR, ConfigConstants.PATCH_SIGNING_KEY);

            IPAddress bindIp = IPAddress.Parse(ConfigConstants.OPTIONS_BINDIP);
            int patchPort = ConfigConstants.OPTIONS_PATCH_PORT;
            int loginPort = ConfigConstants.OPTIONS_LOGIN_PORT;

            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.AddServerHeader = false;
                options.Listen(bindIp, patchPort);
                options.Listen(bindIp, loginPort);
                if (PatchStore.Enabled)
                    options.Listen(bindIp, ConfigConstants.TRACKER_PORT);
            });
            if (PatchStore.Enabled)
                builder.Services.AddHostedService<PatchSeed>();

            ServerVersion serverVersion = ServerVersion.Parse(ConfigConstants.DATABASE_SERVER_VERSION);
            builder.Services.AddDbContext<MeteorDb>(options => options.UseMySql(ConfigConstants.ConnectionString(), serverVersion));
            builder.Services.AddScoped<AccountService>();

            // The pages answer only on the login port.
            builder.Services.AddRazorPages(options => options.Conventions.AddFolderApplicationModelConvention("/",
                model => model.EndpointMetadata.Add(new HostAttribute("*:" + loginPort))));

            WebApplication app = builder.Build();

            // Only login and sign-up need the database, so a bad connection doesn't stop the server.
            Log.Info("Testing DB connection to \"{0}\"... ", ConfigConstants.DATABASE_HOST);
            using (IServiceScope scope = app.Services.CreateScope())
            {
                if (scope.ServiceProvider.GetRequiredService<MeteorDb>().Database.CanConnect())
                    Log.Info("Connection ok.");
                else
                    Log.Error("Can't connect to the database; login and sign-up will fail.");
            }

            app.Use(async (context, next) =>
            {
                await next();
                Log.Info("{0} :{1} {2} {3} -> {4}", context.Connection.RemoteIpAddress,
                    context.Connection.LocalPort, context.Request.Method, context.Request.Path,
                    context.Response.StatusCode);
            });

            // Login page images are Square Enix art, so they're not in git. They go in wwwroot/account/content/img.
            string webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            if (Directory.Exists(webRoot))
            {
                PhysicalFileProvider files = new PhysicalFileProvider(webRoot);
                app.UseWhen(context => context.Connection.LocalPort == loginPort,
                    branch => branch.UseStaticFiles(new StaticFileOptions { FileProvider = files }));
            }
            else
            {
                Log.Warn("No wwwroot folder: the login page will show without its images");
            }

            // Each port answers only its own routes.
            app.MapGet("/patch/vercheck/ffxiv/win32/release/{repo}/{version}",
                    (HttpContext context, string repo, string version) => VersionCheck.Handle(context, repo, version))
                .RequireHost("*:" + patchPort);

            if (PatchStore.Enabled)
                app.MapGet("/announce", Tracker.Announce).RequireHost("*:" + ConfigConstants.TRACKER_PORT);

            app.MapRazorPages();

            Log.Info("Listening on {0}: patch {1}, login {2}", bindIp, patchPort, loginPort);
            app.Run();
        }
    }
}
