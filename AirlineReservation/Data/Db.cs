using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace AirlineReservationDemo.Data
{
    /// <summary>
    /// Thin ADO.NET helper used by every entity and service.
    /// All statements are parameterised and every connection is disposed.
    /// The connection string lives in App.config (name: "AirlineDb").
    /// </summary>
    internal static class Db
    {
        private const string FallbackConnection =
        @"Data Source=.\SQLEXPRESS;Initial Catalog=AirlineReservationDB;Integrated Security=True;Encrypt=False";

        public static string ConnectionString
        {
            get {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["AirlineDb"];
                var builder=new SqlConnectionStringBuilder(settings!=null?settings.ConnectionString:FallbackConnection);
                if(string.IsNullOrWhiteSpace(builder.InitialCatalog)) builder.InitialCatalog="AirlineReservationDB";
                return builder.ConnectionString;
            }
        }

        public static SqlConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>Creates a parameter; null is converted to DBNull.</summary>
        public static SqlParameter P(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        // ------------------------------------------------ stand-alone calls

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection con = CreateConnection()) using (SqlCommand cmd = new SqlCommand(sql, con)) using (
            SqlDataAdapter adapter = new SqlDataAdapter(cmd))
            {
                AddParameters(cmd, parameters);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection con = CreateConnection()) using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                AddParameters(cmd, parameters);
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection con = CreateConnection()) using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                AddParameters(cmd, parameters);
                con.Open();
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        // ------------------------------------------------------ transactions

        /// <summary>
        /// Runs <paramref name="work"/> inside one transaction. The transaction
        /// is committed when the delegate returns and rolled back if it throws.
        /// </summary>
        public static T InTransaction<T>(Func<SqlConnection, SqlTransaction, T> work)
        {
            using (SqlConnection con = CreateConnection())
            {
                con.Open();
                using (SqlTransaction tx = con.BeginTransaction())
                {
                    try
                    {
                        T result = work(con, tx);
                        tx.Commit();
                        return result;
                    }
                    catch
                    {
                        try
                        {
                            tx.Rollback();
                        }
                        catch (Exception rollbackError) when (rollbackError is InvalidOperationException || rollbackError is SqlException)
                        { /* already completed */
                        }
                        throw;
                    }
                }
            }
        }

        public static DataTable QueryTx(SqlConnection con, SqlTransaction tx, string sql,
        params SqlParameter[] parameters)
        {
            using (SqlCommand cmd = new SqlCommand(sql, con, tx)) using (SqlDataAdapter adapter =
            new SqlDataAdapter(cmd))
            {
                AddParameters(cmd, parameters);
                DataTable table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public static int ExecuteTx(SqlConnection con, SqlTransaction tx, string sql, params SqlParameter[] parameters)
        {
            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                AddParameters(cmd, parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object ScalarTx(SqlConnection con, SqlTransaction tx, string sql,
        params SqlParameter[] parameters)
        {
            using (SqlCommand cmd = new SqlCommand(sql, con, tx))
            {
                AddParameters(cmd, parameters);
                object result = cmd.ExecuteScalar();
                return result == DBNull.Value ? null : result;
            }
        }

        // ---------------------------------------------------------- helpers

        /// <summary>True for the SQL Server "duplicate key" error numbers.</summary>
        public static bool IsDuplicateKey(SqlException ex)
        {
            return ex.Number == 2601 || ex.Number == 2627;
        }

        /// <summary>True for a foreign-key violation (row is still referenced).</summary>
        public static bool IsForeignKeyViolation(SqlException ex)
        {
            return ex.Number == 547;
        }

        public static string Like(string text)
        {
            return "%" + (text ?? string.Empty).Trim().Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") +
            "%";
        }

        private static void AddParameters(SqlCommand cmd, SqlParameter[] parameters)
        {
            if (parameters == null)
            return;
            foreach (SqlParameter p in parameters)
            {
                cmd.Parameters.Add(p);
            }
        }
    }
}
