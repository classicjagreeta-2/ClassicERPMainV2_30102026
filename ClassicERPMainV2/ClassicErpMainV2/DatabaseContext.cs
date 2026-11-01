using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Xml.Linq;

namespace ClassicErpMainV2
{
    public class DatabaseContext : DbContext
    {
        // Pass the connection string name to the base constructor
        public DatabaseContext() : base(GetDynamicConnectionStringName()) { }
        public DbSet<Models.MODULE> MODULE { get; set; } 
        public DbSet<Models.USER_JAG> USER_JAG { get; set; }
        public DbSet<Models.USER_SCHEMA_LINK> USER_SCHEMA_LINK { get; set; } 
        public DbSet<Models.CLASSIC_CONTROL> CLASSIC_CONTROL { get; set; } 
        private static string GetDynamicConnectionStringName()
        {
            string _databaseType = WebConfigurationManager.AppSettings["DatabaseType"];
            if (_databaseType == "SQL")
                return "SqlDbContext";
            else
                return "OracleDbContext";
        }

        // Optional: Specify SQL Server Schema (e.g., "dbo")
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            string schemaname = GetSchemaName();
            modelBuilder.HasDefaultSchema(schemaname);
            base.OnModelCreating(modelBuilder);
        }

        public static string GetSchemaName()
        {
            try
            {
                string _databaseType = WebConfigurationManager.AppSettings["DatabaseType"];
                if (_databaseType == "SQL")
                {
                    var connectionString = WebConfigurationManager.ConnectionStrings["SqlDbContext"].ConnectionString.ToString();
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    return "dbo";
                }
                else
                {
                    var connectionString = WebConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString.ToString();
                    var builder = new OracleConnectionStringBuilder(connectionString);
                    return builder.UserID;
                }
            }
            catch (ArgumentException ex)
            {
                // Handle cases where the connection string is malformed or invalid
                Console.WriteLine($"Error parsing connection string: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Handle other potential errors
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return null;
            }
        }
    }
}