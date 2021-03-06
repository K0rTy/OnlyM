﻿namespace OnlyM.Core.Services.Database
{
    using System;
    using System.Data.SQLite;
    using System.IO;
    using System.Text;
    using Serilog;
    using Utils;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class DatabaseService : IDatabaseService
    {
        private const int CurrentSchemaVersion = 2;

        public DatabaseService()
        {
            EnsureDatabaseExists();
        }

        public void ClearThumbCache()
        {
            using (var c = CreateConnection())
            using (var cmd = c.CreateCommand())
            {
                Log.Logger.Verbose("Clearing thumb cache");

                cmd.CommandText = "delete from thumb; PRAGMA vacuum;";
                cmd.ExecuteNonQuery();
            }
        }

        public void AddThumbnailToCache(string originalPath, long originalLastChanged, byte[] thumbnail)
        {
            using (var c = CreateConnection())
            using (var cmd = c.CreateCommand())
            {
                Log.Logger.Verbose($"Inserting into thumb table {originalPath}");

                var sb = new StringBuilder();

                sb.AppendLine("insert into thumb (path, image, changed)");
                sb.AppendLine("select");
                sb.AppendLine($"@P, @T, {originalLastChanged}");
                sb.AppendLine("where not exists(select 1 from thumb where path=@P)");

                cmd.CommandText = sb.ToString();
                cmd.Parameters.AddWithValue("@P", originalPath);
                cmd.Parameters.AddWithValue("@T", thumbnail);

                cmd.ExecuteNonQuery();
            }
        }

        public byte[] GetThumbnailFromCache(string originalPath, long originalLastChanged)
        {
            using (var c = CreateConnection())
            using (var cmd = c.CreateCommand())
            {
                Log.Logger.Verbose($"Selecting from thumb table {originalPath}");

                cmd.CommandText = "select id, image, changed from thumb where path = @P";
                cmd.Parameters.AddWithValue("@P", originalPath);

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        int id = Convert.ToInt32(r["id"]);
                        long lastChanged = Convert.ToInt64(r["changed"]);

                        if (lastChanged != originalLastChanged)
                        {
                            DeleteThumbRow(c, id);
                        }
                        else
                        {
                            return (byte[])r["image"];
                        }
                    }
                }
            }

            return null;
        }

        private void EnsureDatabaseExists()
        {
            var path = GetDatabasePath();
            if (File.Exists(path))
            {
                if (IsUnsupportedVersion())
                {
                    DeleteDatabase();
                }
            }

            if (!File.Exists(path))
            {
                CreateDatabase();

                if (!File.Exists(path))
                {
                    throw new Exception("Could not create database!");
                }
            }
        }

        private string GetDatabasePath()
        {
            return Path.Combine(FileUtils.GetOnlyMDatabaseFolder(), "OnlyMDatabase.db");
        }

        private SQLiteConnection CreateConnection()
        {
            var c = new SQLiteConnection($"Data source={GetDatabasePath()};Version=3;");
            c.Open();
            return c;
        }

        private void DeleteDatabase()
        {
            Log.Logger.Verbose("Deleting database");

            SQLiteConnection.ClearAllPools();
            File.Delete(GetDatabasePath());
        }

        private int GetDatabaseSchemaVersion()
        {
            using (var c = CreateConnection())
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = "select * from pragma_user_version()";
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void SetDatabaseSchemaVersion(SQLiteConnection connection, int version)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA user_version={version}";
                cmd.ExecuteNonQuery();
            }
        }

        private bool IsUnsupportedVersion()
        {
            return GetDatabaseSchemaVersion() != CurrentSchemaVersion;
        }

        private void CreateDatabase()
        {
            using (var c = CreateConnection())
            {
                Log.Logger.Verbose("Creating database");

                CreateThumbTable(c);
                SetDatabaseSchemaVersion(c, CurrentSchemaVersion);
            }
        }

        private void DeleteThumbRow(SQLiteConnection connection, int id)
        {
            using (var cmd = connection.CreateCommand())
            {
                Log.Logger.Verbose("Deleting row from thumb table");

                cmd.CommandText = $"delete from thumb where id = {id}";
                cmd.ExecuteNonQuery();
            }
        }

        private void CreateThumbTable(SQLiteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                Log.Logger.Verbose("Creating thumb table");

                var sb = new StringBuilder();
                sb.AppendLine("CREATE TABLE[thumb](");
                sb.AppendLine("[id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE,");
                sb.AppendLine("[path] TEXT NOT NULL COLLATE NOCASE,");
                sb.AppendLine("[image] BLOB NOT NULL,");
                sb.AppendLine("[changed] INTEGER NOT NULL);");

                sb.AppendLine("CREATE UNIQUE INDEX[PathIndex] ON[thumb]([path]);");

                cmd.CommandText = sb.ToString();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
