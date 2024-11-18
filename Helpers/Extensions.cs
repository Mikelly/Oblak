using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Oblak.Helpers
{
    public static class DbExtensions
    {
        public static decimal GetBalance(this ApplicationDbContext _db, string taxType, int? legalEntity, int? agency)
        {
            using (var command = _db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC CalcBalance @taxType, @legalEntity, @agency";
                command.CommandType = System.Data.CommandType.Text;
                command.Parameters.Add(new SqlParameter("@taxType", SqlDbType.NVarChar) { Value = taxType });
                command.Parameters.Add(new SqlParameter("@legalEntity", SqlDbType.Int) { Value = legalEntity ?? 0 });
                command.Parameters.Add(new SqlParameter("@agency", SqlDbType.Int) { Value = agency ?? 0 });

                _db.Database.OpenConnection();

                var result = command.ExecuteScalar();
                var calc = result != DBNull.Value ? Convert.ToDecimal(result) : 0m;

                _db.Database.CloseConnection();

                return calc;
            }
        }
    }
}
