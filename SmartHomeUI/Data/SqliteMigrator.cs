using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace SmartHomeUI.Data;

public static class SqliteMigrator
{
    public static void EnsureUserColumns()
    {
        try
        {
            using var conn = new SqliteConnection("Data Source=smarthome.db");
            conn.Open();

            string table = "Users";
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Users','User') LIMIT 1;";
                var result = check.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(result)) table = result!;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({table});";
                using var reader = cmd.ExecuteReader();
                var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    cols.Add(reader.GetString(1));
                }

                void AddCol(string name, string sqlTpl)
                {
                    if (!cols.Contains(name))
                    {
                        using var alter = conn.CreateCommand();
                        alter.CommandText = sqlTpl.Replace("{table}", table);
                        alter.ExecuteNonQuery();
                    }
                }

                // Users table columns that newer versions expect
                AddCol("Email", "ALTER TABLE {table} ADD COLUMN Email TEXT NOT NULL DEFAULT '';" );
                AddCol("IsOnline", "ALTER TABLE {table} ADD COLUMN IsOnline INTEGER NOT NULL DEFAULT 0;" );
                AddCol("PasswordHash", "ALTER TABLE {table} ADD COLUMN PasswordHash TEXT NOT NULL DEFAULT '';" );
                AddCol("PasswordSalt", "ALTER TABLE {table} ADD COLUMN PasswordSalt TEXT NOT NULL DEFAULT '';" );
                AddCol("SmartThingsPatEncrypted", "ALTER TABLE {table} ADD COLUMN SmartThingsPatEncrypted TEXT NULL;" );
                AddCol("CreatedAt", "ALTER TABLE {table} ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'));" );
                AddCol("LastLoginAt", "ALTER TABLE {table} ADD COLUMN LastLoginAt TEXT NULL;" );
                AddCol("FailedLoginCount", "ALTER TABLE {table} ADD COLUMN FailedLoginCount INTEGER NOT NULL DEFAULT 0;" );
                AddCol("LockedUntil", "ALTER TABLE {table} ADD COLUMN LockedUntil TEXT NULL;" );
            }
        }
        catch
        {
            // ignore
        }
    }
    public static void EnsureDeviceColumns()
    {
        try
        {
            using var conn = new SqliteConnection("Data Source=smarthome.db");
            conn.Open();

            string table = "Devices";
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN ('Devices','Device') LIMIT 1;";
                var result = check.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(result)) table = result!;
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({table});";
                using var reader = cmd.ExecuteReader();
                var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (reader.Read())
                {
                    cols.Add(reader.GetString(1));
                }

                void AddCol(string name, string sqlTpl)
                {
                    if (!cols.Contains(name))
                    {
                        using var alter = conn.CreateCommand();
                        alter.CommandText = sqlTpl.Replace("{table}", table);
                        alter.ExecuteNonQuery();
                    }
                }

                AddCol("Type", "ALTER TABLE {table} ADD COLUMN Type TEXT NOT NULL DEFAULT '';" );
                AddCol("Room", "ALTER TABLE {table} ADD COLUMN Room TEXT NULL;" );
                AddCol("IsOn", "ALTER TABLE {table} ADD COLUMN IsOn INTEGER NOT NULL DEFAULT 0;" );
                AddCol("IsOnline", "ALTER TABLE {table} ADD COLUMN IsOnline INTEGER NOT NULL DEFAULT 1;" );
                AddCol("Battery", "ALTER TABLE {table} ADD COLUMN Battery INTEGER NOT NULL DEFAULT 100;" );
                AddCol("Value", "ALTER TABLE {table} ADD COLUMN Value REAL NOT NULL DEFAULT 0;" );
                AddCol("Favorite", "ALTER TABLE {table} ADD COLUMN Favorite INTEGER NOT NULL DEFAULT 0;" );
                AddCol("IsPhysical", "ALTER TABLE {table} ADD COLUMN IsPhysical INTEGER NOT NULL DEFAULT 0;" );
                AddCol("PhysicalDeviceId", "ALTER TABLE {table} ADD COLUMN PhysicalDeviceId TEXT NOT NULL DEFAULT '';" );
                AddCol("LastSeen", "ALTER TABLE {table} ADD COLUMN LastSeen TEXT NULL;" );
                AddCol("CreatedAt", "ALTER TABLE {table} ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'));" );
            }

            // Ensure Automations table exists
            using (var create = conn.CreateCommand())
            {
                create.CommandText = "CREATE TABLE IF NOT EXISTS Automations (\n" +
                                     "    Id INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                                     "    Name TEXT NOT NULL,\n" +
                                     "    UserId INTEGER NOT NULL,\n" +
                                     "    DeviceId INTEGER NOT NULL,\n" +
                                     "    TimeHHmm TEXT NOT NULL,\n" +
                                     "    Action TEXT NOT NULL,\n" +
                                     "    Value REAL NOT NULL DEFAULT 0,\n" +
                                     "    Enabled INTEGER NOT NULL DEFAULT 1\n" +
                                     ");";
                create.ExecuteNonQuery();
            }
        }
        catch
        {
            // ignore
        }
    }
}
