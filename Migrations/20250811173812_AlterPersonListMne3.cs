using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterPersonListMne3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@" 
                    ALTER PROCEDURE [dbo].[PersonListMne]
                                                            @property INT,
                                                            @od DATETIME2,
                                                            @do DATETIME2
                                                        AS
                                                        BEGIN
                                                            SELECT 
                                                                p.ID, 
                                                                o.Name AS PropertyName,
                                                                ROW_NUMBER() OVER (ORDER BY p.CheckIn, p.Id) AS No,
                                                                p.FirstName, 
                                                                p.LastName, 
                                                                p.PersonalNumber, 
                                                                p.BirthDate,
                                                                (SELECT cl.Name 
                                                                 FROM CodeLists cl 
                                                                 WHERE cl.Country = 'MNE' 
                                                                   AND cl.ExternalId = p.DocumentType 
                                                                   AND cl.Type = 'isprava') AS DocumentType, 
                                                                p.DocumentNumber, 
                                                                (SELECT cl.Name 
                                                                 FROM CodeLists cl 
                                                                 WHERE cl.Country = 'MNE' 
                                                                   AND cl.ExternalId = p.DocumentCountry 
                                                                   AND cl.Type = 'drzava') AS DocumentCountry,        
                                                                p.CheckIn, 
                                                                p.CheckOut, 
                                                                dbo.Nights(p.Id, @do) AS Nights,
                                                                -- proracun takse
                                                                CAST(
                                                                    CASE 
                                                                        WHEN (DATEDIFF(YEAR, p.BirthDate, p.CheckIn) 
                                                                              - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.BirthDate, p.CheckIn), p.BirthDate) > p.CheckIn THEN 1 ELSE 0 END) >= 18 
                                                                            THEN COALESCE(m.ResidenceTaxAmount, 0) * dbo.Nights(p.Id, @do)  -- FULL
                                                                        WHEN (DATEDIFF(YEAR, p.BirthDate, p.CheckIn) 
                                                                              - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.BirthDate, p.CheckIn), p.BirthDate) > p.CheckIn THEN 1 ELSE 0 END) >= 12 
                                                                            THEN (COALESCE(m.ResidenceTaxAmount, 0) / 2.0) * dbo.Nights(p.Id, @do)  -- HALF
                                                                        ELSE 0  -- NONE
                                                                    END 
                                                                AS DECIMAL(10,2)) AS ResTaxAmount
                                                            FROM MnePersons p
                                                            INNER JOIN Properties o 
                                                                ON o.ID = p.PropertyId
                                                            LEFT  JOIN Municipalities m
                                                                ON m.Id = o.MunicipalityId 
					                                        WHERE o.Id = @property 
						                                        AND p.ExternalId IS NOT NULL
						                                        AND p.CheckIn >= @od               
						                                        AND p.CheckIn < DATEADD(day, 1, @do)  
                                                            ORDER BY p.CheckIn, p.Id;
                                                        END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
