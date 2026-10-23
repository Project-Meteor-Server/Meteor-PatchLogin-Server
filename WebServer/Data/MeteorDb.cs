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
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace Meteor.Web.Data
{
    // The users and sessions tables, shared with the lobby server.
    // The game servers' SQL files own these tables. This code never changes their layout.
    public class MeteorDb : DbContext
    {
        public MeteorDb(DbContextOptions<MeteorDb> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder model)
        {
            model.Entity<User>(user =>
            {
                user.ToTable("users");
                user.HasKey(u => u.Id);
                user.Property(u => u.Id).HasColumnName("id");
                user.Property(u => u.Name).HasColumnName("name").HasMaxLength(User.MaxNameLength);
                user.Property(u => u.PassHash).HasColumnName("passhash").HasMaxLength(56).IsFixedLength();
                user.Property(u => u.Salt).HasColumnName("salt").HasMaxLength(56).IsFixedLength();
                user.Property(u => u.Email).HasColumnName("email").HasMaxLength(User.MaxEmailLength);
                user.HasIndex(u => u.Name).IsUnique();
            });

            model.Entity<Session>(session =>
            {
                session.ToTable("sessions");
                session.HasKey(s => s.Id);
                session.Property(s => s.Id).HasColumnName("id").HasMaxLength(56).IsFixedLength();
                session.Property(s => s.UserId).HasColumnName("userid");
                session.Property(s => s.Expiration).HasColumnName("expiration");
                session.HasIndex(s => s.UserId).IsUnique();
            });
        }

        // The database's clock. The lobby server checks expiry times against it, so we set them by it too.
        public DateTime Now()
        {
            return Database.SqlQuery<DateTime>($"SELECT NOW() AS Value").AsEnumerable().Single();
        }
    }

    public class User
    {
        public const int MaxNameLength = 45;
        public const int MaxEmailLength = 256;

        public int Id { get; set; }
        public string Name { get; set; }
        // SHA-224 of password + salt, in hex.
        public string PassHash { get; set; }
        public string Salt { get; set; }
        public string Email { get; set; }
    }

    public class Session
    {
        // 56 hex characters. One session per user.
        public string Id { get; set; }
        public int UserId { get; set; }
        public DateTime Expiration { get; set; }
    }
}
