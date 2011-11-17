using System;
using System.Data.SqlClient;

namespace Clockz
{
    public class SqlClock : IClock
    {
        readonly string ConnectionString;

        public SqlClock(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public virtual DateTime UtcNow
        {
            get
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();

                using(var cn = new SqlConnection(ConnectionString))
                {
                    cn.Open();
                    using(var cmd = new SqlCommand("select GetUtcDate()", cn))
                    {
                        return DateTime
                            .SpecifyKind((DateTime)cmd.ExecuteScalar(), DateTimeKind.Utc)
                            .AddTicks(sw.ElapsedTicks / 2);
                    }
                }
            }
        }

        public virtual DateTime Today
        {
            get { return UtcNow.Date; }
        }
    }
}
