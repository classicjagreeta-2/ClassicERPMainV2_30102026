using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ClassicErpMainV2.Areas.Admin.Data
{
    public class PasswordAuthentication
    {
        private readonly string _connectionString;

        public PasswordAuthentication(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ChkPassResult ChkPass(string usr, string pwd)
        {
            var result = new ChkPassResult
            {
                CR = "Either incorrect Username or incorrect Password",
                IsSuccess = false
            };

            if (string.IsNullOrWhiteSpace(usr))
                return result;

            using (var con = new OracleConnection(_connectionString))
            {
                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    try
                    {
                        string xUser = usr.PadRight(10, ' ');
                        string xAcstr;
                        string xActTag;
                        decimal xTLoggedOn;
                        string xUserName = "";
                        DateTime xLastLogDt;

                        // Read user information
                        using (var cmd = con.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.BindByName = true;

                            cmd.CommandText = @"
                            SELECT NVL(ACTIVE_TAG, 'R'),
                                   NVL(TIMESLOGGEDON, 1),
                                   SUBSTR(ACCESS_STR, 1, 10),
                                   NVL(USER_NAME, ''),
                                   NVL(LASTLOGGEDON, SYSDATE)
                            FROM USER_JAG
                            WHERE USER_ORA = :USR";

                            cmd.Parameters.Add(
                                "USR", OracleDbType.Varchar2).Value = usr;

                            using (var dr = cmd.ExecuteReader())
                            {
                                if (!dr.Read())
                                {
                                    tx.Rollback();
                                    return result;
                                }

                                xActTag = Convert.ToString(dr.GetValue(0));
                                xTLoggedOn = Convert.ToDecimal(dr.GetValue(1));
                                xAcstr = Convert.ToString(dr.GetValue(2));
                                xUserName = Convert.ToString(dr.GetValue(3));
                                xLastLogDt = Convert.ToDateTime(dr.GetValue(4));
                            }
                        }

                        // Generate the seed exactly as in PL/SQL
                        decimal seed = 0;
                        decimal mult = 1;

                        for (int i = 0; i < 10; i++)
                        {
                            seed +=
                                (int)xUser[i] - (int)'A';

                            seed -= 0; // No-op; keeps seed calculation explicit.
                            seed += 0; // Multiplier is applied below instead.
                            mult *= 1;
                        }

                        // Recalculate seed with the required powers of 5.
                        seed = 0;
                        mult = 1;

                        for (int i = 0; i < 10; i++)
                        {
                            seed += ((int)xUser[i] - (int)'A') * mult;
                            mult *= 5;
                        }

                        decimal[] rand1 = new decimal[10];

                        for (int i = 0; i < 10; i++)
                        {
                            decimal value = Math.Round(
                                (seed * 7137421m),
                                0,
                                MidpointRounding.AwayFromZero);

                            value += 21132487m;

                            decimal rand2 = value / 10000000m;

                            value -= Math.Floor(rand2) * 10000000m;

                            rand1[i] = Math.Floor(value / 100000m);

                            seed += 1;
                        }

                        // Generate MWORD
                        string mWord = "";

                        for (int i = 0; i < 10; i++)
                        {
                            int accessCode = (int)xAcstr[i];
                            int passwordCode = accessCode - (int)rand1[i];

                            if (passwordCode < 0 || passwordCode > 255)
                                throw new InvalidOperationException(
                                    "Generated password contains an invalid character.");

                            mWord += (char)passwordCode;
                        }

                        // Compare passwords (equivalent to TRIM comparison)
                        string suppliedPassword = (pwd ?? "*").Trim();

                        if (!string.Equals(
                            suppliedPassword,
                            mWord.Trim(),
                            StringComparison.Ordinal))
                        {
                            using (var cmd = con.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.BindByName = true;

                                cmd.CommandText = @"
                                INSERT INTO LOGINFAILURE
                                    (USER_CODE, PASSWORD, ATTEMPTEDON)
                                VALUES
                                    (:USER_CODE, :PASSWORD, SYSDATE)";

                                cmd.Parameters.Add(
                                    "USER_CODE", OracleDbType.Varchar2).Value = xUser;

                                cmd.Parameters.Add(
                                    "PASSWORD", OracleDbType.Varchar2).Value =
                                    (object)pwd ?? DBNull.Value;

                                cmd.ExecuteNonQuery();
                            }

                            result.CR =
                                "Either incorrect Username or incorrect Password";

                            result.IsSuccess = false;
                        }
                        else
                        {
                            using (var cmd = con.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.BindByName = true;

                                cmd.CommandText = @"
                                UPDATE USER_JAG
                                SET LASTLOGGEDON = SYSDATE,
                                    TIMESLOGGEDON = TIMESLOGGEDON + 1
                                WHERE USER_ORA = :USR";

                                cmd.Parameters.Add(
                                    "USR", OracleDbType.Varchar2).Value = usr;

                                cmd.ExecuteNonQuery();
                            }

                            result.CR = "Welcome Back";
                            result.IsSuccess = true;
                            result.UserName = xUserName;
                            result.C3 = xLastLogDt;

                            // The original procedure does not assign C1.
                            result.C1 = null;
                        }

                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
    }

    public class ChkPassResult
    {
        public string CR { get; set; }
        public decimal? C1 { get; set; }
        public string UserName { get; set; }
        public DateTime? C3 { get; set; }

        public bool IsSuccess { get; set; }
    }
}