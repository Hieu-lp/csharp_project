namespace FlightBooking.Testing;

internal static class SqlTestPolicyChecks
{
    public static void Run(Action<bool, string> check)
    {
        const string target = "Server=.;Database=FlightBooking_Hieu_ManagementTests;Integrated Security=True";
        const string server = "DESKTOP-1MMSBHT";
        void Refuse(string connection, string? expectedServer, string? approval, string label)
        {
            try { SqlTestPreflight.ValidateSettings(connection, expectedServer, approval); }
            catch (InvalidOperationException) { check(true, label); return; }
            throw new Exception("FAIL: unsafe SQL test settings accepted: " + label);
        }
        Refuse("Server=.;Database=FlightBookingDB;Integrated Security=True", server, SqlTestPreflight.Approval, "SQL gate rejects the main database before connecting");
        Refuse(target.Replace(SqlTestPreflight.DatabaseName, "Other_ManagementTests"), server, SqlTestPreflight.Approval, "SQL gate rejects suffix-only lookalikes");
        Refuse(target, server, null, "SQL gate requires explicit write approval");
        Refuse(target, "", SqlTestPreflight.Approval, "SQL gate requires a verified expected server");
        Refuse(target + ";AttachDbFilename=E:\\unexpected.mdf", server, SqlTestPreflight.Approval, "SQL gate rejects database-file attachment");
        Refuse(target + ";ApplicationIntent=ReadOnly", server, SqlTestPreflight.Approval, "SQL gate rejects unsuitable write connection intent");
        SqlTestPreflight.ValidateSettings(target, server, SqlTestPreflight.Approval);
        check(true, "SQL gate accepts only the exact approved target settings without opening SQL");
    }
}
