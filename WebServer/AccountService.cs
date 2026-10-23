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
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Meteor.Web.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Meteor.Web
{
    // Accounts and logins, stored the way the lobby server reads them.
    // Password hash: SHA-224 of password + salt, in hex. Session id: 56 hex characters, one per user.
    public class AccountService
    {
        readonly MeteorDb db;

        public AccountService(MeteorDb db)
        {
            this.db = db;
        }

        // True when the database is down or refused the query.
        public static bool IsDatabaseError(Exception e)
        {
            return e is DbException || e is DbUpdateException;
        }

        // Returns the user id, or 0 if the name or password is wrong.
        public int VerifyUser(string name, string password)
        {
            User user = db.Users.AsNoTracking().SingleOrDefault(u => u.Name == name);
            if (user == null)
                return 0;

            string hashed = Sha224Hex(password + user.Salt);
            bool ok = CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hashed), Encoding.ASCII.GetBytes(user.PassHash.ToLowerInvariant()));
            return ok ? user.Id : 0;
        }

        public enum CreateResult { Created, NameTaken }

        public CreateResult CreateUser(string name, string password, string email)
        {
            string salt = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(28));
            db.Users.Add(new User { Name = name, PassHash = Sha224Hex(password + salt), Salt = salt, Email = email });
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException e) when (e.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry })
            {
                // The name is already taken.
                return CreateResult.NameTaken;
            }
            return CreateResult.Created;
        }

        // Keeps the user's current session alive longer, or starts a new one.
        public string RefreshOrCreateSession(int userId)
        {
            DateTime now = db.Now();
            DateTime expiration = now.AddHours(ConfigConstants.SESSION_HOURS);

            Session live = db.Sessions.SingleOrDefault(s => s.UserId == userId && s.Expiration > now);
            if (live != null)
            {
                live.Expiration = expiration;
                db.SaveChanges();
                return live.Id;
            }

            db.Sessions.Where(s => s.UserId == userId).ExecuteDelete();
            Session session = new Session
            {
                Id = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(28)),
                UserId = userId,
                Expiration = expiration,
            };
            db.Sessions.Add(session);
            db.SaveChanges();
            return session.Id;
        }

        public static string Sha224Hex(string text)
        {
            return Convert.ToHexStringLower(Sha224.Hash(Encoding.UTF8.GetBytes(text)));
        }
    }
}
