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
using System.Collections.Generic;
using System.IO;
using System.Text;

using Meteor.Web.Patching;
using Microsoft.AspNetCore.Http;

namespace Meteor.Web
{
    // ffxivboot's update check. It checks boot first, then the game, then starts ffxivlogin.
    static class VersionCheck
    {
        // Retail's boundary. Never quote it: boot reads everything after "boundary=" to the line end.
        const string Boundary = "477D80B1_38BC_41d4_8B48_5273ADB89CAC";

        public static IResult Handle(HttpContext context, string repo, string version)
        {
            if (!PatchStore.RepositoryIds.TryGetValue(repo, out string repoId))
                return Results.NotFound();

            string target = PatchCatalog.Target(repo);
            switch (PatchCatalog.Compare(repo, version))
            {
                case PatchCatalog.Standing.Behind:
                    return OfferPatches(context, repo, repoId, version, target);
                case PatchCatalog.Standing.Ahead:
                    // Patches only go forward. Boot has its own error message for 410.
                    Program.Log.Info("vercheck {0}: {1} is newer than {2}", repo, version, target);
                    return Results.StatusCode(StatusCodes.Status410Gone);
                case PatchCatalog.Standing.Unknown:
                    Program.Log.Info("vercheck {0}: {1} isn't a known version", repo, version);
                    return Results.NotFound();
            }

            // The same reply retail gave an up-to-date install.
            WriteHeaders(context.Response, repo, repoId, target);
            Program.Log.Info("vercheck {0}: {1} is up to date", repo, version);
            return Results.Text("", "text/plain", null, StatusCodes.Status204NoContent);
        }

        // One part per patch the install needs: the retail torrent, the patch size and our signature.
        // Boot saves each torrent at its Content-Location under downloads\, checks every signature,
        // downloads the patches, and has ffxivupdater install them.
        static IResult OfferPatches(HttpContext context, string repo, string repoId, string version, string target)
        {
            List<string> pending = PatchCatalog.Pending(repo, version);
            List<PatchStore.Patch> patches = new List<PatchStore.Patch>();
            foreach (string next in pending)
            {
                PatchStore.Patch patch = PatchStore.Find(repo, next);
                if (patch == null)
                {
                    // Boot shows its generic error for a 404.
                    Program.Log.Info("vercheck {0}: {1} is behind {2}, but there's no signed torrent for {3}", repo, version, target, next);
                    return Results.NotFound();
                }
                patches.Add(patch);
            }

            using (MemoryStream body = new MemoryStream())
            {
                foreach (PatchStore.Patch patch in patches)
                {
                    Write(body, "--" + Boundary + "\r\n");
                    Write(body, "Content-Type: application/octet-stream\r\n");
                    Write(body, "Content-Location: ffxiv/" + repoId + "/metainfo/D" + patch.Version + ".torrent\r\n");
                    Write(body, "X-Patch-Length: " + patch.Length + "\r\n");
                    Write(body, "X-Signature: " + patch.Signature + "\r\n");
                    Write(body, "\r\n");
                    // No line break before the next boundary: boot takes everything up to it as the
                    // torrent, and checks the signature over exactly those bytes.
                    body.Write(patch.Torrent, 0, patch.Torrent.Length);
                }
                Write(body, "--" + Boundary + "--\r\n");

                WriteHeaders(context.Response, repo, repoId, target);
                Program.Log.Info("vercheck {0}: {1} is behind {2}, offering {3} patch(es)", repo, version, target, patches.Count);
                return Results.Bytes(body.ToArray(), "multipart/mixed; boundary=" + Boundary);
            }
        }

        // Boot fails if any of these headers is missing, on every reply.
        static void WriteHeaders(HttpResponse response, string repo, string repoId, string target)
        {
            response.Headers["Content-Location"] = "ffxiv/" + repoId + "/vercheck.dat";
            response.Headers["X-Repository"] = "ffxiv/win32/release/" + repo;
            response.Headers["X-Patch-Module"] = "ZiPatch";
            response.Headers["X-Protocol"] = "torrent";
            response.Headers["X-Info-Url"] = ConfigConstants.PATCH_INFO_URL;
            response.Headers["X-Latest-Version"] = target;
        }

        static void Write(MemoryStream stream, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
