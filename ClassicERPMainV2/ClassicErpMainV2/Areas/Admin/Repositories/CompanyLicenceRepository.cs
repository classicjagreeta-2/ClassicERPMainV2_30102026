using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using ClassicErpMainV2.Areas.Admin.Data;

namespace ClassicErpMainV2.Areas.Admin.Repositories
{
    public class CompanyLicenceRepository
    {
        private readonly string _connectionString;
        private readonly string _dbType;        // "oracle" | "sqlserver" — was database_type
        public string _schemaPrefix { get; set; }
        public string _menuTablePrefix { get; set; }
        public string _usracsTablePrefix { get; set; }
        public string _unitTablePrefix { get; set; }
        public string _unitFieldPrefix { get; set; }
        public string _moduleNamePrefix { get; set; }
        public string _moduleCodePrefix { get; set; }
        public CompanyLicenceRepository(string connectionString)
        {
            _connectionString = connectionString;
            _dbType = (System.Configuration.ConfigurationManager.AppSettings["DatabaseType"] ?? "sqlserver")
                        .Trim().ToLowerInvariant();
            // Load current user's session context
            LoadSessionContext();

        }

        private bool IsOracle => _dbType == "oracle";
        // Optional runtime context updater
        private void LoadSessionContext()
        {
            var session = HttpContext.Current?.Session;

            if (session == null)
                return;

            _schemaPrefix =
                session["SCHEMA"] as string ?? "";
            _menuTablePrefix =
                session["MENU_TABLE"] as string ?? "";
            _usracsTablePrefix =
                session["USRACS_TABLE"] as string ?? "";
            _unitTablePrefix =
                session["UNIT_TABLE"] as string ?? "";
            _unitFieldPrefix =
                session["UNIT_FIELD"] as string ?? "";
            _moduleNamePrefix =
                session["MODULE_NAME"] as string ?? "";
            _moduleCodePrefix =
                session["MODULE_CODE"] as string ?? "";
        }

        public void SetContextPrefix(string schemaPrefix, string menuTablePrefix, string usracsTablePrefix, string unitFieldPrefix,
                string unitTablePrefix, string moduleNamePrefix, string moduleCodePrefix)
        {
            _schemaPrefix = schemaPrefix ?? "";
            _menuTablePrefix = menuTablePrefix ?? "";
            _usracsTablePrefix = usracsTablePrefix ?? "";
            _unitTablePrefix = unitTablePrefix ?? "";
            _unitFieldPrefix = unitFieldPrefix ?? "";
            _moduleNamePrefix = moduleNamePrefix ?? "";
            _moduleCodePrefix = moduleCodePrefix ?? "";
        }

        public static string ComposeSchemaPrefix(string module, string company, string year) =>
            $"{module}_{company}{year}";

        public List<SAAS_COMPANY_LICENCE> GetAll()
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SAAS_COMPANY_LICENCE";
                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToList<SAAS_COMPANY_LICENCE>();
                }
            }
        }

        public SAAS_COMPANY_LICENCE GetById(int id)
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SAAS_COMPANY_LICENCE WHERE ID = @ID";

                var p = cmd.CreateParameter();
                p.ParameterName = "@ID";
                p.Value = id;
                cmd.Parameters.Add(p);

                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToSingle<SAAS_COMPANY_LICENCE>();
                }
            }
        }

        public int Insert(SAAS_COMPANY_LICENCE entity)
        {
            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                var sql = @"
                    INSERT INTO SAAS_COMPANY_LICENCE 
                    (COMPANY_CODE, COMPANY_NAME, PLAN_NAME, MAX_USERS, MAX_CONCURRENT_USERS, EXPIRY_DATE, IS_ACTIVE, CREATED_DATE)
                    VALUES 
                    (@COMPANY_CODE, @COMPANY_NAME, @PLAN_NAME, @MAX_USERS, @MAX_CONCURRENT_USERS, @EXPIRY_DATE, @IS_ACTIVE, GETDATE());
                    SELECT SCOPE_IDENTITY();";
                int rowsAffected = Exec(cnn, tx, sql,
                              ("@COMPANY_CODE", entity.COMPANY_CODE),
                               ("@COMPANY_NAME", entity.COMPANY_NAME),
                               ("@PLAN_NAME", entity.PLAN_NAME),
                               ("@MAX_USERS", entity.MAX_USERS),
                               ("@MAX_CONCURRENT_USERS", entity.MAX_CONCURRENT_USERS),
                               ("@EXPIRY_DATE", entity.EXPIRY_DATE),
                               ("@IS_ACTIVE", entity.IS_ACTIVE ?? "Y"));

                tx.Commit();
                return Convert.ToInt32(rowsAffected > 0);
            }
        }

        public bool Update(SAAS_COMPANY_LICENCE entity)
        {
            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {
                    var sql = @"
                    UPDATE SAAS_COMPANY_LICENCE 
                    SET COMPANY_CODE = @COMPANY_CODE,
                        COMPANY_NAME = @COMPANY_NAME,
                        PLAN_NAME = @PLAN_NAME,
                        MAX_USERS = @MAX_USERS,
                        MAX_CONCURRENT_USERS = @MAX_CONCURRENT_USERS,
                        EXPIRY_DATE = @EXPIRY_DATE,
                        IS_ACTIVE = @IS_ACTIVE
                    WHERE ID = @ID";
                    var now = DateTime.Now;
                    int rowsAffected = Exec(cnn, tx, sql,
                        ("@ID", entity.ID),
                        ("@COMPANY_CODE", entity.COMPANY_CODE),
                        ("@COMPANY_NAME", entity.COMPANY_NAME),
                        ("@PLAN_NAME", entity.PLAN_NAME),
                        ("@MAX_USERS", entity.MAX_USERS),
                        ("@MAX_CONCURRENT_USERS", entity.MAX_CONCURRENT_USERS),
                        ("@EXPIRY_DATE", entity.EXPIRY_DATE),
                        ("@IS_ACTIVE", entity.IS_ACTIVE));
                    tx.Commit();
                    return rowsAffected > 0;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }


        public bool Delete(int id)
        {
            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {
                    Exec(cnn, tx, $"DELETE FROM SAAS_COMPANY_LICENCE WHERE ID = @ID", ("@ID", id));
                    tx.Commit();
                    return true;
                }
                catch (Exception)
                {

                    throw;
                }
      
            }
        }

        private IDbConnection Open()
        {
            IDbConnection cnn = IsOracle
                ? (IDbConnection)new OracleConnection(_connectionString)
                : new SqlConnection(_connectionString);
            cnn.Open();
            return cnn;
        }
        private string AdaptSql(string sql) => IsOracle ? sql.Replace("@", ":") : sql;
        private IDbCommand CreateCommand(string sql, IDbConnection cnn, IDbTransaction tx = null)
        {
            var cmd = cnn.CreateCommand();
            cmd.CommandText = AdaptSql(sql);
            if (tx != null) cmd.Transaction = tx;
            if (cmd is OracleCommand oracleCmd) oracleCmd.BindByName = true;
            return cmd;
        }
        private void AddParam(IDbCommand cmd, string name, object value)
        {
            {
                var op = new OracleParameter
                {
                    ParameterName = name.TrimStart('@', ':'),
                    Direction = ParameterDirection.Input,
                    OracleDbType = MapOracleDbType(value)
                };
                op.Value = value ?? DBNull.Value;
                cmd.Parameters.Add(op);
                return;
            }

            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
        private static OracleDbType MapOracleDbType(object value)
        {
            switch (value)
            {
                case null:
                    return OracleDbType.Varchar2;
                case string _:
                    return OracleDbType.Varchar2;
                case int _:
                case short _:
                    return OracleDbType.Int32;
                case long _:
                    return OracleDbType.Int64;
                case decimal _:
                case double _:
                case float _:
                    return OracleDbType.Decimal;
                case DateTime _:
                    return OracleDbType.Date;
                case bool _:
                    return OracleDbType.Int16; // Oracle has no native BOOLEAN bind type for SQL (PL/SQL only)
                default:
                    return OracleDbType.Varchar2;
            }
        }
        private IDbCommand Command(string sql, IDbConnection cnn, IDbTransaction tx, params (string name, object value)[] parms)
        {
            var cmd = CreateCommand(sql, cnn, tx);
            foreach (var p in parms) AddParam(cmd, p.name, p.value);
            return cmd;
        }
        private int Exec(IDbConnection cnn, IDbTransaction tx, string sql, params (string name, object value)[] parms)
        {
            using (var cmd = Command(sql, cnn, tx, parms))
            {
                return cmd.ExecuteNonQuery();
            }
        }
    }

    public class SAAS_COMPANY_LICENCE
    {
        public virtual int ID { get; set; }
        public virtual string COMPANY_CODE { get; set; }
        public virtual string COMPANY_NAME { get; set; }
        public virtual string PLAN_NAME { get; set; }
        public virtual decimal MAX_USERS { get; set; }
        public virtual decimal MAX_CONCURRENT_USERS { get; set; }
        public virtual DateTime EXPIRY_DATE { get; set; }
        public virtual string IS_ACTIVE { get; set; }
        public virtual DateTime CREATED_DATE { get; set; } = System.DateTime.Now;
    }
}