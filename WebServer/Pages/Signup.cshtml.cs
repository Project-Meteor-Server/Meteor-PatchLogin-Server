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
using System.Net.Mail;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Meteor.Web.Pages
{
    // Account sign-up. Reached from the login page's link, in the same window.
    public class SignupModel : PageModel
    {
        readonly AccountService accounts;

        public SignupModel(AccountService accounts)
        {
            this.accounts = accounts;
        }

        [BindProperty(Name = "username")]
        public string Username { get; set; }

        [BindProperty(Name = "password")]
        public string Password { get; set; }

        [BindProperty(Name = "repeatPassword")]
        public string RepeatPassword { get; set; }

        [BindProperty(Name = "email")]
        public string Email { get; set; }

        public string Error { get; private set; }
        public bool Created { get; private set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            Username = Username?.Trim() ?? "";
            Email = Email?.Trim() ?? "";
            string password = Password?.Trim() ?? "";
            string repeatPassword = RepeatPassword?.Trim() ?? "";

            if (Username.Length == 0)
                return Fail("You must enter a username.");
            if (Username.Length > Data.User.MaxNameLength)
                return Fail("Your username can be at most " + Data.User.MaxNameLength + " characters.");
            if (password.Length == 0)
                return Fail("You must enter a password.");
            if (password != repeatPassword)
                return Fail("Repeated password doesn't match with entered password.");
            if (Email.Length == 0 || Email.Length > Data.User.MaxEmailLength || !MailAddress.TryCreate(Email, out _))
                return Fail("You must enter a valid e-mail address.");

            try
            {
                if (accounts.CreateUser(Username, password, Email) == AccountService.CreateResult.NameTaken)
                    return Fail("That username is already taken.");
            }
            catch (Exception e) when (AccountService.IsDatabaseError(e))
            {
                Program.Log.Error(e.ToString());
                return Fail("The server can't create accounts right now.");
            }

            Program.Log.Info("signup: created \"{0}\"", Username);
            Created = true;
            return Page();
        }

        IActionResult Fail(string error)
        {
            Error = error;
            return Page();
        }
    }
}
