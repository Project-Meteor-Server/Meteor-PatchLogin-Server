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
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meteor.Web.Pages
{
    // The page ffxivlogin shows in its small built-in browser.
    // After each page load, ffxivlogin looks for an <x-sqexauth> tag. If it finds one, it starts the
    // game with the tag's sid, lang, region and utc.
    // There's no way to report an error: the form just stays up, and the player can press Cancel.
    // The retail page had no anti-forgery token, so this one doesn't either.
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        static readonly string[] Languages = { "ja-jp", "en-us", "en-gb", "fr-fr", "de-de" };

        // The last username typed in, so the form is filled in next time. ffxivlogin keeps cookies.
        const string RememberCookie = "username";

        readonly AccountService accounts;

        public LoginModel(AccountService accounts)
        {
            this.accounts = accounts;
        }

        [BindProperty(Name = "username")]
        public string Username { get; set; }

        [BindProperty(Name = "password")]
        public string Password { get; set; }

        [BindProperty(Name = "remember")]
        public bool Remember { get; set; }

        public string Error { get; private set; }

        public string Language { get; private set; }
        public LoginText Text { get; private set; }

        // Set after a good login. The page then includes the <x-sqexauth> tag.
        public string SessionId { get; private set; }
        public long Utc { get; private set; }

        public void OnGet()
        {
            PickLanguage();
            Username = Request.Cookies[RememberCookie];
            Remember = true;
        }

        public void OnPost()
        {
            PickLanguage();
            Username = Username?.Trim() ?? "";
            string password = Password?.Trim() ?? "";
            RememberUsername();

            if (Username.Length == 0)
            {
                Error = Text.NoUsername;
                return;
            }
            if (password.Length == 0)
            {
                Error = Text.NoPassword;
                return;
            }

            try
            {
                int userId = accounts.VerifyUser(Username, password);
                if (userId == 0)
                {
                    Program.Log.Info("login: bad name or password for \"{0}\"", Username);
                    Error = Text.BadLogin;
                    return;
                }
                SessionId = accounts.RefreshOrCreateSession(userId);
                Program.Log.Info("login: user {0} \"{1}\" signed in", userId, Username);
            }
            catch (Exception e) when (AccountService.IsDatabaseError(e))
            {
                Program.Log.Error(e.ToString());
                Error = Text.ServerDown;
                return;
            }

            Utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // ?lang= lets a normal browser preview the other languages.
        void PickLanguage()
        {
            string query = Request.Query["lang"];
            Language = Array.IndexOf(Languages, query) >= 0 ? query : LanguageFrom(Request.Headers.UserAgent.ToString());
            Text = LoginText.For(Language);
        }

        void RememberUsername()
        {
            if (!Remember)
            {
                Response.Cookies.Delete(RememberCookie, new CookieOptions { Path = "/account/content" });
                return;
            }
            if (Username.Length == 0)
                return;
            Response.Cookies.Append(RememberCookie, Username, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Path = "/account/content",
                Expires = DateTimeOffset.UtcNow.AddYears(1),
            });
        }

        // ffxivlogin's User-Agent is "SQEXAuthor/2.0.0(<os>; <lang>; <machine id>)".
        static string LanguageFrom(string userAgent)
        {
            Match m = Regex.Match(userAgent, @"^SQEXAuthor/[^(]*\([^;]*;\s*([a-z]{2}-[a-z]{2})\s*;");
            if (m.Success && Array.IndexOf(Languages, m.Groups[1].Value) >= 0)
                return m.Groups[1].Value;
            return "en-us";
        }
    }
}
