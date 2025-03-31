using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using System.Data;
using Microsoft.Data.SqlClient;
using Telerik.SvgIcons;
using Kendo.Mvc.Extensions;

namespace Oblak.Helpers
{
    public static class DbExtensions
    {
        public static decimal GetBalance(this ApplicationDbContext _db, string taxType, int? legalEntity, int? agency)
        {
            using (var command = _db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT dbo.CalcBalance(@taxType, @legalEntity, @agency)";
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
    public static class CodeListExtensions
    {
        public static IQueryable<CodeList> AddMappedLocation(this IQueryable<CodeList> data, string? externalId, string newLocationName, string existingLocationName)
        {
            var dataLst = data.ToList();

            if (string.IsNullOrEmpty(externalId) &&
                dataLst.Where(x => x.Type == "mjesto" && x.Param1 == externalId).Any(x => x.Name == newLocationName))
                return dataLst.AsQueryable();

            var existingLocation = dataLst.FirstOrDefault(x => x.Type == "mjesto" && x.Param1 == externalId && x.Name == existingLocationName);
            if (existingLocation != null)
            {
                var mappedLocation = new CodeList
                {
                    Id = dataLst.Max(a => a.Id) + 1,
                    Name = newLocationName,
                    Type = "mjesto",
                    ExternalId = existingLocation.ExternalId,
                    Param1 = existingLocation.Param1,
                    Param2 = existingLocation.Param2,
                    Param3 = existingLocation.Param3,
                    Country = existingLocation.Country
                };

                return dataLst.Append(mappedLocation).AsQueryable();
            }

            return dataLst.AsQueryable();
        }
    }
}
