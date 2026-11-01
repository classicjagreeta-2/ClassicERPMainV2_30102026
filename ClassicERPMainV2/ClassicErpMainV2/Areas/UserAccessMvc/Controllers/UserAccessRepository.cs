using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using ClassicErpMainV2.Areas.UserAccessMvc.Models;
using System.Diagnostics;
using ClassicErpMainV2.Models;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web;

namespace ClassicErpMainV2.Areas.UserAccessMvc.Data
{
    /// <summary>
    /// Supports SQL Server and Oracle from one code path, matching the VB6 form's
    /// "If database_type = oracle Then ... Else ..." branching — but centralized here
    /// instead of repeated at every call site. Two things differ per provider and are
    /// handled explicitly:
    ///   1. Parameter placeholder syntax: SQL Server uses "@name", Oracle uses ":name".
    ///      All SQL below is written with "@name" and adapted at execution time.
    ///   2. A handful of functions have no common ANSI equivalent (SUBSTRING/ISNULL vs
    ///      SUBSTR/NVL) — those queries are branched explicitly, same as the legacy code.
    ///
    /// Everything is parameterized — the legacy code concatenated user input straight
    /// into SQL strings (e.g. "...WHERE uid_ora='" & Text1.Text & "'"), which is a
    /// SQL-injection hole. That's fixed here; keep it that way.
    /// </summary>
    public class UserAccessRepository
    {
        private readonly string _connectionString;
        private readonly string _dbType;        // "oracle" | "sqlserver" — was database_type
        public string _schemaPrefix { get; set; }
        public string _menuTablePrefix { get; set; }
        public string _usracsTablePrefix { get; set; }
        public string _unitTablePrefix { get; set; }
        public string _moduleNamePrefix { get; set; }
        public string _moduleCodePrefix { get; set; }
        public UserAccessRepository(string connectionString)
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
            _moduleNamePrefix =
                session["MODULE_NAME"] as string ?? "";
            _moduleCodePrefix =
                session["MODULE_CODE"] as string ?? "";
        }

        public void SetContextPrefix(string schemaPrefix, string menuTablePrefix, string usracsTablePrefix,
                string unitTablePrefix, string moduleNamePrefix, string moduleCodePrefix)
        {
            _schemaPrefix = schemaPrefix ?? "";
            _menuTablePrefix = menuTablePrefix ?? "";
            _usracsTablePrefix = usracsTablePrefix ?? "";
            _unitTablePrefix = unitTablePrefix ?? "";
            _moduleNamePrefix = moduleNamePrefix ?? "";
            _moduleCodePrefix = moduleCodePrefix ?? "";
        }

        public static string ComposeSchemaPrefix(string module, string company, string year) =>
            $"{module}_{company}{year}"; // ASSUMPTION — confirm this matches your real naming convention

        // ---- Module / Company / Year cascade for the rights-loading context -----------
        // No table for any of these existed in the VB6 code shown, so these are pragmatic
        // stand-ins — swap the bodies below for real lookups once you tell me where
        // modules/companies/years actually live (a MAST_MODULE/MAST_COMPANY/MAST_YEAR
        // table, a config file, etc).
        public List<MODULE> LoadModules()
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM MODULE WHERE TECHNOLOGY = 'MVC'";
                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToList<MODULE>();
                }
            }
        }

        public List<CLASSIC_CONTROL> LoadClassicControlForModule(string module = "")
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CLASSIC_CONTROL ";
                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToList<CLASSIC_CONTROL>();
                }
            }
        }

        public List<CLASSIC_CONTROL> LoadYearsForCompany(string module, string company)
        {
            using (var cnn = Open())
            using (var cmd = cnn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM CLASSIC_CONTROL ";
                using (var rdr = cmd.ExecuteReader())
                {
                    return rdr.MapToList<CLASSIC_CONTROL>();
                }
            }
        }

        // ---- connection / command factory --------------------------------------------

        private IDbConnection Open()
        {
            IDbConnection cnn = IsOracle
                ? (IDbConnection)new OracleConnection(_connectionString)
                : new SqlConnection(_connectionString);
            cnn.Open();
            return cnn;
        }

        /// <summary>
        /// Rewrites "@name" placeholders to ":name" for Oracle. Safe here because none of
        /// the SQL text below otherwise contains a literal "@".
        /// </summary>
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
            // Oracle-specific: going through the generic IDbDataParameter interface
            // (CreateParameter()/Value=) leaves OracleDbType unset, and ODP.NET's managed
            // driver can fail type inference at execute time with "ORA-50028: Invalid
            // parameter binding" rather than defaulting sensibly. Setting OracleDbType
            // explicitly avoids it — this is the documented fix for that error.
            if (cmd is OracleCommand)
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

        // ---- menu tree (was Rs1 = SELECT * FROM VBMENU_FIN order by ordercode) --------
        public List<MenuNode> LoadMenuTree()
        {
            var flat = new List<MenuNode>();
            using (var cnn = Open())
            using (var cmd = Command(
                $"SELECT ID, PID, INDEX_ORA, NAME, TYPE, DTLOCK, IMAGE FROM  VBMENU_FIN ORDER BY ORDERCODE",
                cnn, null))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    flat.Add(new MenuNode
                    {
                        Id = rdr["ID"].ToString().Trim().ToUpper(),
                        ParentId = rdr["PID"].ToString().Trim().ToUpper(),
                        IndexOra = rdr["INDEX_ORA"].ToString().Trim(),
                        Name = rdr["NAME"].ToString().Trim(),
                        Type = rdr["TYPE"].ToString().Trim(),
                        DtLock = rdr["DTLOCK"].ToString().Trim() == "Y",
                        Image = rdr["IMAGE"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["IMAGE"])
                    });
                }
            }
            return BuildTree(flat);
        }

        private List<MenuNode> BuildTree(List<MenuNode> flat)
        {
            // Keyed by the legacy tree-node key (ID & "!" & INDEX_ORA, see Form_Load's `x`
            // variable) — NOT by Id alone. The same ID repeats across sibling rows that only
            // differ by INDEX_ORA (e.g. three "MST" rows), so a plain Id-keyed Dictionary
            // throws on the duplicate keys; MenuNode.Key already encodes Id+IndexOra the
            // same way the VB6 node key did.
            var byKey = new Dictionary<string, MenuNode>();
            foreach (var node in flat)
            {
                // Last one wins on a genuine duplicate key rather than throwing, since a
                // crash here takes down the whole rights screen for every user.
                byKey[node.Key] = node;
            }

            var roots = new List<MenuNode>();
            foreach (var node in flat)
            {
                if (string.IsNullOrEmpty(node.ParentId) || !byKey.ContainsKey(node.ParentId) || node.ParentId == node.Key)
                    roots.Add(node);
                else
                    byKey[node.ParentId].Children.Add(node);
            }
            return roots;
        }

        // ---- rights already granted to a user (was TREEUPDATE typ="U" / RSUSR filter) --
        public List<MenuRight> LoadUserRights(string userOra) =>
            LoadRights($"{_schemaPrefix}.USRACS_FIN", "UID_ORA", userOra);

        public List<MenuRight> LoadGroupRights(string groupId) =>
            LoadRights($"{_schemaPrefix}.GRPACS_FIN", "UID_ORA", groupId);

        private List<MenuRight> LoadRights(string table, string keyColumn, string keyValue)
        {
            var result = new List<MenuRight>();
            using (var cnn = Open())
            using (var cmd = Command(
                $"SELECT MNUID, INDEX_ORA, AEDV, ADAY, EDAY, DDAY FROM {table} WHERE {keyColumn} = @key",
                cnn, null, ("@key", keyValue)))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    result.Add(new MenuRight
                    {
                        MnuId = rdr["MNUID"].ToString().Trim(),
                        IndexOra = rdr["INDEX_ORA"].ToString().Trim(),
                        Aedv = rdr["AEDV"].ToString().Trim(),
                        AddLockDays = rdr["ADAY"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["ADAY"]),
                        EditLockDays = rdr["EDAY"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["EDAY"]),
                        DeleteLockDays = rdr["DDAY"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["DDAY"])
                    });
                }
            }
            return result;
        }

        // ---- duplicate checks (was RSTST.Find in SAVE_Click / text1_Validate) ----------
        public bool UserExists(string userOra)
        {
            using (var cnn = Open())
            using (var cmd = Command(
                $"SELECT COUNT(1) FROM  USER_JAG WHERE USER_ORA = @u", cnn, null, ("@u", userOra)))
            {
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        public bool GroupExists(string groupId)
        {
            using (var cnn = Open())
            using (var cmd = Command(
                $"SELECT COUNT(1) FROM  {_schemaPrefix}.GRPACS_FIN WHERE UID_ORA = @g", cnn, null, ("@g", groupId)))
            {
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        // ---- units assignable to a user (was RsUnit) -----------------------------------
        // Dialect-specific: SQL Server SUBSTRING/ISNULL vs Oracle SUBSTR/NVL — no common
        // ANSI equivalent worth abstracting for two functions, so branch explicitly like
        // the legacy `If database_type = "oracle" Then ... Else ...` did.
        public List<UnitLink> LoadAssignableUnits()
        {
            string sql = IsOracle
                ? $"SELECT CODE, NAME FROM {_schemaPrefix}.{_unitTablePrefix} WHERE NVL(DIRE,' ') <> 'Y' " +
                  "AND SUBSTR(CODE,4,1) <> 'C' AND SUBSTR(CODE,4,1) <> 'D' ORDER BY NAME"
                : $"SELECT CODE, NAME FROM {_schemaPrefix}.{_unitTablePrefix} WHERE ISNULL(DIRE,' ') <> 'Y' " +
                  "AND SUBSTRING(CODE,4,1) <> 'C' AND SUBSTRING(CODE,4,1) <> 'D' ORDER BY NAME";

            var units = new List<UnitLink>();
            using (var cnn = Open())
            using (var cmd = Command(sql, cnn, null))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    units.Add(new UnitLink { UnitCode = rdr["CODE"].ToString().Trim(), UnitName = rdr["NAME"].ToString().Trim() });
            }
            return units;
        }

        public List<UnitLink> LoadUnitsForUser(string userOra)
        {
            var qry = $@"SELECT a.UNIT, b.NAME FROM USER_SCHEMA_LINK a
                   JOIN {_schemaPrefix}.{_unitTablePrefix} b ON a.UNIT = b.CODE
                   WHERE a.USER_ORA = '{userOra}' AND a.USR_ID = '{_schemaPrefix}' AND a.MODULE_NAME = '{_moduleNamePrefix}'
                   ORDER BY b.CODE";
            var units = new List<UnitLink>();
            using (var cnn = Open())
            using (var cmd = Command(qry, cnn, null))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    units.Add(new UnitLink { UnitCode = rdr["UNIT"].ToString().Trim(), UnitName = rdr["NAME"].ToString().Trim() });
            }
            return units;
        }

        // ---- voucher types + which are already granted (was RSMSTVIEW1 / RS_CHK) ------
        public List<VoucherTypeRight> LoadVoucherRights(string userOra, string loca)
        {
            var list = new List<VoucherTypeRight>();
            using (var cnn = Open())
            using (var cmd = Command($"SELECT TYPE, DESC_ORA FROM {_schemaPrefix}.BHVT WHERE DRCR <> 0 ORDER BY TYPE", cnn, null))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                    list.Add(new VoucherTypeRight { Type = rdr["TYPE"].ToString().Trim(), DescOra = rdr["DESC_ORA"].ToString().Trim() });
            }

            var granted = new HashSet<string>();
            if (!string.IsNullOrEmpty(loca))
            {
                using (var cnn = Open())
                using (var cmd = Command(
                    $"SELECT VOU_TYPE FROM {_schemaPrefix}.MAST_VOU_RIGHT WHERE UID_ORA = @u AND LOCA = @loca",
                    cnn, null, ("@u", userOra), ("@loca", loca)))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                        granted.Add(rdr["VOU_TYPE"].ToString().Trim());
                }
            }
            foreach (var v in list) v.Selected = granted.Contains(v.Type);
            return list;
        }

        // ---- save (was SAVE_Click) — one transaction, delete-then-reinsert pattern ----
        public void SaveUserAccess(UserAccessRecord record, string encodedPassword, string editedByUser,
            string loca, string _unitId, bool isEdit)
        {
            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {
                    var now = DateTime.Now;

                    if (isEdit)
                    {
                        // legacy audit pattern: stamp the outgoing row "E" (edited-out) before delete,
                        // rather than a true UPDATE — preserved here for audit-trail parity.
                        Exec(cnn, tx,
                            $"UPDATE {_schemaPrefix}.USRACS_FIN SET USER_NEW=@u, ENTDT_NEW=@p_d, TIME_NEW=@p_t, DTAG='E' WHERE UID_ORA=@id",
                            ("@u", editedByUser), ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")), ("@id", record.UserOra));
                        Exec(cnn, tx, $"DELETE FROM {_schemaPrefix}.USRACS_FIN WHERE UID_ORA=@id", ("@id", record.UserOra));

                        Exec(cnn, tx,
                            $"UPDATE  USER_JAG SET USER_NEW=@u, ENTDT_NEW=@p_d, TIME_NEW=@p_t, DTAG='E' WHERE USER_ORA=@id",
                            ("@u", editedByUser), ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")), ("@id", record.UserOra));
                        Exec(cnn, tx, $"DELETE FROM  USER_JAG WHERE USER_ORA=@id", ("@id", record.UserOra));
                    }

                    Exec(cnn, tx,
                        $@"INSERT INTO  USER_JAG
                           (USER_ORA, GRP, PASSWORD, ACCESS_STR, USER_ENT, ENTDT, TIME_ORA, USERCREATED, USER_NAME,
                            MAIL_USER_NAME, MAIL_PASSWORD, SMTP_SERVER, SMTP_PORT)
                           VALUES (@id, @p_grp, @p_pwd, @p_enc, @p_by, @p_d, @p_t, @created, @name, @mail, @mailpwd, @smtp, @port)",
                        ("@id", record.UserOra), ("@p_grp", record.Group ?? ""), ("@p_pwd", new string('*', 10)),
                        ("@p_enc", encodedPassword), ("@p_by", editedByUser), ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")),
                        ("@created", now), ("@name", record.UserName), ("@mail", record.EmailAddress ?? ""),
                        ("@mailpwd", record.SmtpPassword ?? ""), ("@smtp", record.SmtpServer ?? ""), ("@port", record.SmtpPort ?? ""));

                    Exec(cnn, tx,
                        $@"DELETE FROM  USER_SCHEMA_LINK
                           WHERE USER_ORA=@id AND USR_ID=@unit AND MODULE_NAME='{_moduleNamePrefix}'",
                        ("@id", record.UserOra), ("@unit", _unitId));

                    if (record.AssignedUnits.Count == 0)
                    {
                        Exec(cnn, tx,
                            $@"INSERT INTO  USER_SCHEMA_LINK
                               (LOCA, UNIT, USER_ORA, USR_ID, MODULE_NAME, USER_ENT, ENTDT, TIME_ORA,MODULE_CODE)
                               VALUES (@loca, '', @id, @unit, '{_moduleNamePrefix}', @p_by, @p_d, @p_t,'{_moduleCodePrefix}')",
                            ("@loca", loca), ("@id", record.UserOra), ("@unit", _unitId), ("@p_by", editedByUser),
                            ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")));
                    }
                    else
                    {
                        foreach (var u in record.AssignedUnits)
                        {
                            // duplicate-unit guard was Command3_Click's manual loop-compare;
                            // enforced in the controller before this is ever called (see Save action).
                            Exec(cnn, tx,
                                $@"INSERT INTO  USER_SCHEMA_LINK
                                   (LOCA, UNIT, USER_ORA, USR_ID, MODULE_NAME, USER_ENT, ENTDT, TIME_ORA,MODULE_CODE)
                                   VALUES (@loca, @unit_code, @id, @unit, '{_moduleNamePrefix}', @p_by, @p_d, @p_t,'{_moduleCodePrefix}')",
                                ("@loca", loca), ("@unit_code", u.UnitCode), ("@id", record.UserOra), ("@unit", _unitId),
                                ("@p_by", editedByUser), ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")));
                        }
                    }

                    Exec(cnn, tx, $"DELETE FROM {_schemaPrefix}.USRACS_FIN WHERE UID_ORA=@id AND UID_ORA <> ''",
                        ("@id", record.UserOra));
                    foreach (var r in record.Rights)
                    {
                        Exec(cnn, tx,
                            $@"INSERT INTO {_schemaPrefix}.USRACS_FIN (UID_ORA, MNUID, INDEX_ORA, AEDV, EDAY, ADAY, DDAY, SCHEMA_NM)
                               VALUES (@id, @mnu, @idx, @aedv, @eday, @aday, @p_dday, @unit)",
                            ("@id", record.UserOra), ("@mnu", r.MnuId), ("@idx", r.IndexOra), ("@aedv", r.Aedv),
                            ("@eday", r.EditLockDays), ("@aday", r.AddLockDays), ("@p_dday", r.DeleteLockDays), ("@unit", _unitId));
                    }

                    Exec(cnn, tx, $"DELETE FROM {_schemaPrefix}.MAST_VOU_RIGHT WHERE UID_ORA=@id", ("@id", record.UserOra));
                    foreach (var v in record.VoucherRights.Where(v => v.Selected))
                    {
                        Exec(cnn, tx,
                            $@"INSERT INTO {_schemaPrefix}.MAST_VOU_RIGHT (LOCA, UID_ORA, VOU_TYPE, USER_NAME, USER_ENTDT)
                              VALUES (@loca, @id, @p_type, @p_by, @p_d)",
                            ("@loca", loca), ("@id", record.UserOra), ("@p_type", v.Type), ("@p_by", editedByUser), ("@p_d", now.AddDays(1).Date));
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    var stackTrace = new StackTrace(ex, true);
                    var frame = stackTrace.GetFrame(0); // Get top stack frame

                    int lineNumber = frame.GetFileLineNumber();
                    string fileName = frame.GetFileName();

                    tx.Rollback();
                    throw;
                }
            }
        }

        public void DeleteUser(string userOra, string editedByUser)
        {
            using (var cnn = Open())
            using (var tx = cnn.BeginTransaction())
            {
                try
                {
                    var now = DateTime.Now;
                    Exec(cnn, tx,
                        $"UPDATE {_schemaPrefix}.USRACS_FIN SET USER_NEW=@u, ENTDT_NEW=@p_d, TIME_NEW=@p_t, DTAG='E' WHERE UID_ORA=@id",
                        ("@u", editedByUser), ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")), ("@id", userOra));
                    Exec(cnn, tx, $"DELETE FROM {_schemaPrefix}.USRACS_FIN WHERE UID_ORA=@id", ("@id", userOra));

                    Exec(cnn, tx,
                        $"UPDATE  USER_JAG SET USER_NEW=@u, ENTDT_NEW=@p_d, TIME_NEW=@p_t, DTAG='E' WHERE USER_ORA=@id",
                        ("@u", editedByUser), ("@p_d", now.Date), ("@p_t", now.ToString("HH:mm")), ("@id", userOra));
                    Exec(cnn, tx, $"DELETE FROM  USER_JAG WHERE USER_ORA=@id", ("@id", userOra));

                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        // ---- full user record for the Index/rights screen (was Text1_Change assembling
        // Text2/Text3/Text5/user_name/Txt* + the rights/units/vouchers TREEUPDATE calls) ----
        public UserAccessRecord LoadUserRecord(string userOra, string loca)
        {
            UserAccessRecord record = null;
            using (var cnn = Open())
            using (var cmd = Command(
                $@"SELECT *
                   FROM  USER_JAG WHERE USER_ORA = @u",
                cnn, null, ("@u", userOra)))
            using (var rdr = cmd.ExecuteReader())
            {
                if (rdr.Read())
                {
                    record = new UserAccessRecord
                    {
                        UserOra = rdr["USER_ORA"].ToString().Trim(),
                        UserName = rdr["USER_NAME"] == DBNull.Value ? "" : rdr["USER_NAME"].ToString().Trim(),
                        Group = rdr["GRP"] == DBNull.Value ? "" : rdr["GRP"].ToString().Trim(),
                        Password = rdr["ACCESS_STR"] == DBNull.Value ? "" : rdr["ACCESS_STR"].ToString().Trim(),
                        EmailAddress = rdr["MAIL_USER_NAME"] == DBNull.Value ? "" : rdr["MAIL_USER_NAME"].ToString().Trim(),
                        SmtpServer = rdr["SMTP_SERVER"] == DBNull.Value ? "" : rdr["SMTP_SERVER"].ToString().Trim(),
                        SmtpPort = rdr["SMTP_PORT"] == DBNull.Value ? "" : rdr["SMTP_PORT"].ToString().Trim(),
                        IsNewRecord = false
                    };
                }
            }

            if (record == null) return null; // caller decides how to treat "not found" (e.g. stale link)

            // Password is intentionally never read back — the view only ever shows a masked
            // placeholder for an existing user and leaves it blank for editing.
            //record.Rights = LoadUserRights(userOra);
            //record.AssignedUnits = LoadUnitsForUser(userOra);
            //record.VoucherRights = LoadVoucherRights(userOra, loca);
            return record;
        }

        // First user in USER_ORA order — was RSUSR.MoveFirst's default selection in Form_Load.
        public string LoadFirstUserOra()
        {
            using (var cnn = Open())
            using (var cmd = Command($"SELECT MIN(USER_ORA) FROM  USER_JAG", cnn, null))
            {
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value || result == null ? null : result.ToString().Trim();
            }
        }

        // Group ids for the "Group" dropdown — was the distinct UID_ORA values GRPACS_FIN holds.
        // If you maintain a separate group-description table, join it here and return display
        // names instead; see the same note on UserListItem.GroupName.
        public List<string> LoadGroups()
        {
            var groups = new List<string>();
            using (var cnn = Open())
            using (var cmd = Command(
                $"SELECT DISTINCT UID_ORA FROM  GRPACS_FIN ORDER BY UID_ORA", cnn, null))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read()) groups.Add(rdr["UID_ORA"].ToString().Trim());
            }
            return groups;
        }

        // ---- listing grid (new — the legacy form only ever showed one user via Text1 navigation) ----
        // Two assumptions called out explicitly since neither existed in the VB6 schema shown:
        //   1. USER_JAG.IS_ACTIVE — add this column (bit/number(1)) if you want a real status
        //      flag; without it every user reads as Active.
        //   2. Group display name — the legacy GRPACS_FIN only stores the group id (GRP), not a
        //      label. If you have a separate group-description table, join it in here instead
        //      of falling back to the raw id.
        public List<UserListItem> LoadUserList()
        {
            string lengthFn = IsOracle ? "LENGTH" : "LEN";
            bool hasActiveColumn = HasColumn($" USER_JAG", "IS_ACTIVE");

            string activeExpr = hasActiveColumn ? "u.IS_ACTIVE" : "1";
            string sql = $@"
                SELECT
                    u.USER_ORA, u.USER_NAME, u.GRP, u.MAIL_USER_NAME AS EMAIL,
                    {activeExpr} AS IS_ACTIVE
                FROM USER_JAG u
                ORDER BY u.USER_ORA";

            //string sql = $@"
            //    SELECT
            //        u.USER_ORA, u.USER_NAME, u.GRP, u.MAIL_USER_NAME AS EMAIL,
            //        {activeExpr} AS IS_ACTIVE,
            //        COALESCE(r.GRANTED, 0) AS GRANTED
            //    FROM USER_JAG u
            //    LEFT JOIN (
            //        SELECT UID_ORA, SUM({lengthFn}(AEDV)) AS GRANTED
            //        FROM {_schemaPrefix}.USRACS_FIN
            //        GROUP BY UID_ORA
            //    ) r ON r.UID_ORA = u.USER_ORA
            //    ORDER BY u.USER_ORA";

            var list = new List<UserListItem>();
            using (var cnn = Open())
            using (var cmd = Command(sql, cnn, null))
            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    list.Add(new UserListItem
                    {
                        UserOra = rdr["USER_ORA"].ToString().Trim(),
                        UserName = rdr["USER_NAME"] == DBNull.Value ? "" : rdr["USER_NAME"].ToString().Trim(),
                        GroupId = rdr["GRP"] == DBNull.Value ? "" : rdr["GRP"].ToString().Trim(),
                        GroupName = rdr["GRP"] == DBNull.Value ? "" : rdr["GRP"].ToString().Trim(), // see note above
                        Email = rdr["EMAIL"] == DBNull.Value ? "" : rdr["EMAIL"].ToString().Trim(),
                        IsActive = Convert.ToInt32(rdr["IS_ACTIVE"]) != 0,
                        //RightsGranted = rdr["GRANTED"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["GRANTED"])
                    });
                }
            }

            int total = LoadTotalRightSlots();
            foreach (var u in list) u.RightsTotal = total;
            return list;
        }

        // Total possible A/E/D/V grants = (# leaf menu nodes) * 4, matching the "76" in the
        // 19-item mock menu (19 leaves * 4 letters = 76).
        private int LoadTotalRightSlots()
        {
            using (var cnn = Open())
            using (var cmd = Command(
            $"SELECT COUNT(*) FROM  VBMENU_FIN WHERE TYPE IN ('E','E1')", cnn, null))
            {
                return Convert.ToInt32(cmd.ExecuteScalar()) * 4;
            }
        }

        private bool HasColumn(string table, string column)
        {
            try
            {
                string sql = IsOracle
                    ? "SELECT COUNT(*) FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = UPPER(@p_t) AND COLUMN_NAME = UPPER(@c)"
                    : "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @p_t AND COLUMN_NAME = @c";
                using (var cnn = Open())
                using (var cmd = Command(sql, cnn, null, ("@p_t", table), ("@c", column)))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
            catch
            {
                return false; // fail safe to "no status column" rather than blow up the listing page
            }
        }

        // Generic across SqlConnection/SqlTransaction and OracleConnection/OracleTransaction —
        // routes through Command()/AddParam() so placeholder adaptation and BindByName apply here too.
        private void Exec(IDbConnection cnn, IDbTransaction tx, string sql, params (string name, object value)[] parms)
        {
            using (var cmd = Command(sql, cnn, tx, parms))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}