using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AddProcCountryStatsPeriod : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
                    CREATE PROCEDURE [dbo].[CountryStatsPeriod]
                        @firmaid INT,
                        @datefrom DATETIME,
                        @dateto DATETIME
                    AS
                    BEGIN
                        SET NOCOUNT ON;

                        SELECT CountryDesc, 
                               SUM(Arrival) AS Arrivals, 
                               SUM(Guest) AS Guests, 
                               SUM(Nights) AS Nights, 
                               SUM(ResTaxAmount) AS ResTax, 
                               ROUND(CAST(SUM(Guest) AS DECIMAL(10,2)) / CAST(TotalGuests AS DECIMAL(10,2)), 4) * 100 AS CountryGuestShare, 
                               ROUND(CAST(SUM(Arrival) AS DECIMAL(10,2)) / CAST(TotalArrivals AS DECIMAL(10,2)), 4) * 100 AS CountryArrivalShare, 
                               ROUND(CAST(SUM(Nights) AS DECIMAL(10,2)) / CAST(TotalNights AS DECIMAL(10,2)), 4) * 100 AS CountryNightsShare, 
                               TotalGuests, 
                               TotalArrivals, 
                               TotalNights 
                        FROM
                        (
                            SELECT    
                                m.DocumentCountry AS Country,    
                                (SELECT cl.Name 
                                 FROM CodeLists cl 
                                 WHERE cl.Country = 'MNE' 
                                   AND cl.Type = 'drzava' 
                                   AND cl.ExternalId = m.DocumentCountry) AS CountryDesc,
                                CASE WHEN CAST(m.CheckIn AS DATE) BETWEEN @datefrom AND @dateto THEN 1 ELSE 0 END AS Arrival,
                                SUM(CASE WHEN CAST(m.CheckIn AS DATE) BETWEEN @datefrom AND @dateto THEN 1 ELSE 0 END) OVER (PARTITION BY 1) AS TotalArrivals,
                                1 AS Guest,
                                COUNT(*) OVER (PARTITION BY 1) AS TotalGuests,
                                DATEDIFF(DAY, 
                                    CASE WHEN @datefrom > CAST(m.CheckIn AS DATE) THEN @datefrom ELSE CAST(m.CheckIn AS DATE) END, 
                                    CASE WHEN @dateto < CAST(COALESCE(m.CheckOut, @dateto) AS DATE) THEN @dateto ELSE CAST(COALESCE(m.CheckOut, @dateto) AS DATE) END
                                ) AS Nights,
                                SUM(
                                    DATEDIFF(DAY, 
                                        CASE WHEN @datefrom > CAST(m.CheckIn AS DATE) THEN @datefrom ELSE CAST(m.CheckIn AS DATE) END, 
                                        CASE WHEN @dateto < CAST(COALESCE(m.CheckOut, @dateto) AS DATE) THEN @dateto ELSE CAST(COALESCE(m.CheckOut, @dateto) AS DATE) END
                                    )
                                ) OVER (PARTITION BY 1) AS TotalNights,
                                m.ResTaxAmount
                            FROM MnePersons m 
                                INNER JOIN Properties p ON m.PropertyId = p.Id 
                                INNER JOIN LegalEntities le ON le.Id = p.LegalEntityId
                                LEFT JOIN ResTaxTypes rt ON rt.Id = m.ResTaxTypeId
                                LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = m.ResTaxPaymentTypeId
                            WHERE 
                                le.PartnerId = 2 AND 
                                le.Id = @firmaid AND 
                                CAST(m.CheckIn AS DATE) <= CAST(@dateto AS DATE) AND
                                CAST(m.CheckOut AS DATE) >= CAST(@datefrom AS DATE)
                        ) a
                        GROUP BY CountryDesc, TotalGuests, TotalArrivals, TotalNights
                        ORDER BY 3 DESC;
                    END
                    ";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE [dbo].[CountryStatsPeriod]");
        }
    }
}
