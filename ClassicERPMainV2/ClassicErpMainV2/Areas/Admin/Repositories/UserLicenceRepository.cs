using ClassicErpMainV2.Areas.Admin.Data;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Areas.Admin.Repositories
{
    public class UserLicenceRepository
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
        public UserLicenceRepository(string connectionString)
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
        public List<SAAS_USER_LOGIN_SESSION> GetAll()
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SAAS_USER_LOGIN_SESSION";
                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToList<SAAS_USER_LOGIN_SESSION>();
                }
            }
        }

        public SAAS_USER_LOGIN_SESSION GetById(int id)
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM SAAS_USER_LOGIN_SESSION WHERE ID = @ID";
                AddParameter(cmd, "@ID", id);

                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToSingle<SAAS_USER_LOGIN_SESSION>();
                }
            }
        }

        public int Insert(SAAS_USER_LOGIN_SESSION entity)
        {
            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {

                    var sql = @"
                    INSERT INTO SAAS_USER_LOGIN_SESSION 
                    (USER_NAME, COMPANY_CODE, COMPANY_NAME, LOGIN_TIME, LAST_ACTIVITY, LOGOUT_TIME, IS_ONLINE)
                    VALUES 
                    (@USER_NAME, @COMPANY_CODE, @COMPANY_NAME, @LOGIN_TIME, @LAST_ACTIVITY, @LOGOUT_TIME, @IS_ONLINE);
                    SELECT SCOPE_IDENTITY();";
                    int rowsAffected = Exec(cnn, tx, sql,
                   ("@USER_NAME", entity.USER_NAME),
                    ("@COMPANY_CODE", entity.COMPANY_CODE),
                    ("@COMPANY_NAME", entity.COMPANY_NAME),
                    ("@LOGIN_TIME", entity.LOGIN_TIME == default ? DateTime.UtcNow : entity.LOGIN_TIME),
                    ("@LAST_ACTIVITY", entity.LAST_ACTIVITY == default ? DateTime.UtcNow : entity.LAST_ACTIVITY),
                    ("@LOGOUT_TIME", entity.LOGOUT_TIME == entity.LOGOUT_TIME),
                    ("@IS_ONLINE", entity.IS_ONLINE ?? "Y"));

                    tx.Commit();
                    return Convert.ToInt32(rowsAffected > 0);
                }
                catch (Exception)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public bool Update(SAAS_USER_LOGIN_SESSION entity)
        {


            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {

                    var sql = @"
                    UPDATE SAAS_USER_LOGIN_SESSION 
                    SET USER_NAME = @USER_NAME,
                        COMPANY_CODE = @COMPANY_CODE,
                        COMPANY_NAME = @COMPANY_NAME,
                        LOGIN_TIME = @LOGIN_TIME,
                        LAST_ACTIVITY = @LAST_ACTIVITY,
                        LOGOUT_TIME = @LOGOUT_TIME,
                        IS_ONLINE = @IS_ONLINE
                    WHERE ID = @ID";
                    int rowsAffected = Exec(cnn, tx, sql,
                  ("@ID", entity.ID),
                  ("@USER_NAME", entity.USER_NAME),
                  ("@COMPANY_CODE", entity.COMPANY_CODE),
                  ("@COMPANY_NAME", entity.COMPANY_NAME),
                  ("@LOGIN_TIME", entity.LOGIN_TIME),
                  ("@LAST_ACTIVITY", entity.LAST_ACTIVITY),
                  ("@LOGOUT_TIME", entity.LOGOUT_TIME),
                  ("@IS_ONLINE", entity.IS_ONLINE));

                    tx.Commit();
                    return rowsAffected > 0;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public bool Logout(int id)
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE SAAS_USER_LOGIN_SESSION 
                    SET IS_ONLINE = 'N', LOGOUT_TIME = GETDATE()
                    WHERE ID = @ID";

                AddParameter(cmd, "@ID", id);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Delete(int id)
        {

            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {
                    var now = DateTime.Now;
                    Exec(cnn, tx, $"DELETE FROM SAAS_USER_LOGIN_SESSION WHERE ID = @ID", ("@ID", id));
                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
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
        private void AddParameter(IDbCommand cmd, string name, object value)
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
            foreach (var p in parms) AddParameter(cmd, p.name, p.value);
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
    public class SAAS_USER_LOGIN_SESSION
    {
        public virtual int ID { get; set; }
        public virtual string USER_NAME { get; set; }
        public virtual string COMPANY_CODE { get; set; }
        public virtual string COMPANY_NAME { get; set; }
        public virtual DateTime LOGIN_TIME { get; set; }
        public virtual DateTime LAST_ACTIVITY { get; set; }
        public virtual DateTime LOGOUT_TIME { get; set; }
        public virtual string IS_ONLINE { get; set; }
    }
}
