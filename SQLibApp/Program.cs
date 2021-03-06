﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Northwnd;
using SQLibApp.Data;
using System;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace SQLibApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var regions = new[]
            {
                QueryRegion_Traditional(1),
                QueryRegion_SQLib(1),
                QueryRegion_SQLib2(1),
                QueryRegion_EF(1),
            };

            SQLiteDataAdapter adapter = new SQLiteDataAdapter();
            Console.WriteLine(regions.All(x => x == "Eastern"));
        }

        static string QueryRegion_Traditional(int regionId)
        {
            string ret;

            var conn = new SqliteConnection("filename=northwnd.db");
            conn.Open();

            var cmd = new SqliteCommand("SELECT * FROM Regions WHERE RegionId=@p0;", conn);
            cmd.Parameters.Add(new SqliteParameter
            {
                ParameterName = "@p0",
                Value = regionId,
                DbType = DbType.Int32,
            });

            var reader = cmd.ExecuteReader();
            reader.Read();
            ret = reader.GetString("RegionDescription");
            reader.Close();

            conn.Close();

            return ret;
        }

        static string QueryRegion_SQLib(int regionId)
        {
            using (var sqlite = ApplicationDbScope.UseDefault())
            {
                var region = sqlite.SqlQuery($"SELECT * FROM Regions WHERE RegionId={regionId};").First();
                return region["RegionDescription"] as string;
            }
        }

        static string QueryRegion_SQLib2(int regionId)
        {
            using (var sqlite = ApplicationDbScope.UseDefault())
            {
                var region = sqlite.SqlQuery<Region>($"SELECT * FROM Regions WHERE RegionId={regionId};").First();
                return region.RegionDescription;
            }
        }

        static string QueryRegion_EF(int regionId)
        {
            var options = new DbContextOptionsBuilder().UseSqlite("filename=northwnd.db").Options;
            using (var context = new NorthwndContext(options))
            {
                return context.Regions.First(x => x.RegionID == regionId).RegionDescription;
            }
        }

    }
}
