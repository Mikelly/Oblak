using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AddChoiceToPersonListMne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE [dbo].[PersonListMne]
    @property INT,
    @od       DATETIME2,
    @do       DATETIME2,
    @choice   nvarchar(256)
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
        (
            SELECT cl.Name 
            FROM CodeLists cl 
            WHERE cl.Country    = 'MNE'
              AND cl.ExternalId = p.DocumentType
              AND cl.Type       = 'isprava'
        ) AS DocumentType, 
        p.DocumentNumber, 
        (
            SELECT cl.Name 
            FROM CodeLists cl 
            WHERE cl.Country    = 'MNE'
              AND cl.ExternalId = p.DocumentCountry
              AND cl.Type       = 'drzava'
        ) AS DocumentCountry,        
        p.CheckIn, 
        p.CheckOut, 
        dbo.Nights(p.Id, @do) AS Nights,
        CAST(
            CASE 
                WHEN
                    (DATEDIFF(YEAR, p.BirthDate, p.CheckIn)
                     - CASE 
                         WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.BirthDate, p.CheckIn), p.BirthDate) > p.CheckIn 
                         THEN 1 ELSE 0 
                       END
                    ) >= 18
                THEN
                    COALESCE(
                        m.ResidenceTaxAmount,
                        CASE WHEN o.MunicipalityId IS NULL THEN 1.00 ELSE 0 END
                    ) * dbo.Nights(p.Id, @do)
                WHEN
                    (DATEDIFF(YEAR, p.BirthDate, p.CheckIn)
                     - CASE 
                         WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.BirthDate, p.CheckIn), p.BirthDate) > p.CheckIn 
                         THEN 1 ELSE 0 
                       END
                    ) >= 12
                THEN
                    ( COALESCE(
                          m.ResidenceTaxAmount,
                          CASE WHEN o.MunicipalityId IS NULL THEN 1.00 ELSE 0 END
                      ) / 2.0
                    ) * dbo.Nights(p.Id, @do)
                ELSE 0
            END 
        AS DECIMAL(10,2)) AS ResTaxAmount
    FROM MnePersons p
    INNER JOIN Properties o 
        ON o.ID = p.PropertyId
    LEFT JOIN Municipalities m
        ON m.Id = o.MunicipalityId
    WHERE o.Id = @property
      AND p.ExternalId IS NOT NULL
      AND p.CheckIn   >= @od
      AND p.CheckIn   <  DATEADD(day, 1, @do)
      AND (
            (p.Nationality = 'MNE'  AND @choice = 'Domaci')
         OR (p.Nationality <> 'MNE' AND @choice = 'Strani')
         OR  @choice = 'Svi'
      )
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
