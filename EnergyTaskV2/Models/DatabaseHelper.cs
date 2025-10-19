using System.Data.SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyTask.Models
{
    public static class DatabaseHelper
    {
        private static string dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Energy.db"
    );
        private static string connectionString = $"Data Source={dbPath};Version=3;";

        static DatabaseHelper()
        {
            // Копируем базу при первом запуске
            if (!File.Exists(dbPath))
            {
                string sourceDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Energy.db");
                if (File.Exists(sourceDb))
                {
                    File.Copy(sourceDb, dbPath, true);
                }
            }
        }

        public static SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }

    }
}
