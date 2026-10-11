using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using AirlineReservationDemo.Services;

namespace AirlineReservationDemo.Data
{
    internal static class DbInitializer
    {
        private const string ScriptResource="AirlineReservation.sql";
        private const string ScriptDatabaseName="AirlineReservationDB";
        private const string RequiredSchema=@"
            SELECT CASE WHEN
              COL_LENGTH(N'dbo.users',N'MustChangePassword') IS NOT NULL AND
              COL_LENGTH(N'dbo.PassengerTbl',N'ProfilePhoto') IS NOT NULL AND
              OBJECT_ID(N'dbo.PasswordResetRequests',N'U') IS NOT NULL AND
              OBJECT_ID(N'dbo.PasswordResetAudit',N'U') IS NOT NULL AND
              OBJECT_ID(N'dbo.role',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.users',N'U') IS NOT NULL AND
              OBJECT_ID(N'dbo.AirportTbl',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PassengerTbl',N'U') IS NOT NULL AND
              OBJECT_ID(N'dbo.FlightTbl',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.BookingTbl',N'U') IS NOT NULL AND
              OBJECT_ID(N'dbo.TicketTbl',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.PaymentTbl',N'U') IS NOT NULL AND
              OBJECT_ID(N'dbo.vw_FlightAvailability',N'V') IS NOT NULL AND
              COL_LENGTH(N'dbo.users',N'PasswordHash') IS NOT NULL AND
              COL_LENGTH(N'dbo.FlightTbl',N'BusinessSeats') IS NOT NULL AND
              COL_LENGTH(N'dbo.TicketTbl',N'BookingId') IS NOT NULL AND
              EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.TicketTbl') AND name=N'UX_TicketTbl_ActiveSeat') AND
              EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PaymentTbl') AND name=N'UX_Payment_BookingType')
            THEN 1 ELSE 0 END";

        public static void EnsureCreated()
        {
            var builder=new SqlConnectionStringBuilder(Db.ConnectionString);
            string databaseName=builder.InitialCatalog;
            if(!Regex.IsMatch(databaseName,@"^[A-Za-z][A-Za-z0-9_]{0,127}$"))
            throw new InvalidOperationException("Use a database name containing only letters, digits and underscores.");
            builder.InitialCatalog="master";
            using(SqlConnection con=new SqlConnection(builder.ConnectionString))
            {
                con.Open();
                string lockName="SkyWay.Setup."+databaseName;
                using(var cmd=new SqlCommand(@"DECLARE @result int;
                    EXEC @result=sys.sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=120000;
                    SELECT @result;",con))
                {
                    cmd.CommandTimeout=130;cmd.Parameters.AddWithValue("@resource",lockName);
                    if(Convert.ToInt32(cmd.ExecuteScalar())<0) throw new InvalidOperationException("Another instance is preparing this database. Retry in a moment.");
                }
                try
                {
                    bool exists;
                    using(var cmd=new SqlCommand("SELECT DB_ID(@name)",con)) {cmd.Parameters.AddWithValue("@name",databaseName);exists=cmd.ExecuteScalar()!=DBNull.Value;}
                    if(exists)
                    {
                        using(var cmd=new SqlCommand("USE ["+databaseName+"]; SELECT CASE WHEN (OBJECT_ID(N'dbo.FlightTbl',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.FlightTbl',N'BusinessSeats') IS NULL) OR (OBJECT_ID(N'dbo.users',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.users',N'PasswordHash') IS NULL) THEN 1 ELSE 0 END",con))
                        {
                            if(Convert.ToInt32(cmd.ExecuteScalar())==1)
                            throw new InvalidOperationException("This is the original legacy database. Configure a new database such as AirlineReservationDB. Existing records have not been changed.");
                        }
                    }
                    bool ready=false;
                    if(exists) using(var cmd=new SqlCommand(RequiredSchema,con)) ready=Convert.ToInt32(cmd.ExecuteScalar())==1;
                    if(!ready)
                    {
                        using(var cmd=new SqlCommand("USE master",con)) cmd.ExecuteNonQuery();
                        string script=LoadScript().Replace(ScriptDatabaseName,databaseName);
                        foreach(string batch in SplitBatches(script)) using(var cmd=new SqlCommand(batch,con)) {cmd.CommandTimeout=120;cmd.ExecuteNonQuery();}
                    }
                    AuthService.EnsureDefaultAccounts();
                }
                finally
                {
                    // Disposing the connection also releases session locks if a network failure occurred.
                    if(con.State==System.Data.ConnectionState.Open)
                    using(var cmd=new SqlCommand("USE master; EXEC sys.sp_releaseapplock @Resource=@resource,@LockOwner='Session'",con))
                    {
                        cmd.Parameters.AddWithValue("@resource",lockName);
                        try {cmd.ExecuteNonQuery();} catch(SqlException) { }
                    }
                }
            }
        }

        private static string LoadScript()
        {
            using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource))
            {
                if(stream==null) throw new InvalidOperationException("The embedded database script was not found.");
                using(var reader=new StreamReader(stream)) return reader.ReadToEnd();
            }
        }

        internal static IEnumerable<string> SplitBatches(string script)
        {
            foreach(string part in Regex.Split(script,@"^[ \t]*GO[ \t]*\r?$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
            if(!string.IsNullOrWhiteSpace(part)) yield return part;
        }
    }
}
