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

namespace Meteor.Web.Pages
{
    // The login page's words, one set per ffxivlogin language. The images carry no text, so a
    // new language only needs a new entry here.
    public class LoginText
    {
        public string Username { get; init; }
        public string Password { get; init; }
        public string OneTimePassword { get; init; }
        public string Remember { get; init; }
        public string LogIn { get; init; }
        public string NoAccount { get; init; }
        public string NoUsername { get; init; }
        public string NoPassword { get; init; }
        public string BadLogin { get; init; }
        public string ServerDown { get; init; }

        static readonly LoginText English = new LoginText
        {
            Username = "Username",
            Password = "Password",
            OneTimePassword = "One-Time Password",
            Remember = "Remember username",
            LogIn = "Log In",
            NoAccount = "Don't have an account?",
            NoUsername = "You must enter a username.",
            NoPassword = "You must enter a password.",
            BadLogin = "Incorrect username or password.",
            ServerDown = "The server can't sign you in right now.",
        };

        static readonly LoginText Japanese = new LoginText
        {
            Username = "ユーザー名",
            Password = "パスワード",
            OneTimePassword = "ワンタイムパスワード",
            Remember = "ユーザー名を保存する",
            LogIn = "ログイン",
            NoAccount = "アカウントをお持ちでない方",
            NoUsername = "ユーザー名を入力してください。",
            NoPassword = "パスワードを入力してください。",
            BadLogin = "ユーザー名またはパスワードが正しくありません。",
            ServerDown = "現在ログインできません。しばらくしてから再度お試しください。",
        };

        // French and German fall back to English until someone translates them.
        public static LoginText For(string language) => language == "ja-jp" ? Japanese : English;
    }
}
