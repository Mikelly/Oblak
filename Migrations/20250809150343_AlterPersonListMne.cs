using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterPersonListMne : Migration
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
                                    THEN m.ResidenceTaxAmount * dbo.Nights(p.Id, @do)  -- FULL
                                WHEN (DATEDIFF(YEAR, p.BirthDate, p.CheckIn) 
                                      - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, p.BirthDate, p.CheckIn), p.BirthDate) > p.CheckIn THEN 1 ELSE 0 END) >= 12 
                                    THEN (m.ResidenceTaxAmount / 2.0) * dbo.Nights(p.Id, @do)  -- HALF
                                ELSE 0  -- NONE
                            END 
                        AS DECIMAL(10,2)) AS ResTaxAmount
                    FROM MnePersons p
                    INNER JOIN Properties o 
                        ON o.ID = p.PropertyId
                    INNER JOIN Municipalities m
                        ON m.Id = o.MunicipalityId
                    WHERE o.Id = @property 
                      AND p.CheckIn >= @od 
                      AND p.CheckIn <= @do 
                      AND p.ExternalId IS NOT NULL
                    ORDER BY p.CheckIn, p.Id;
                END
            "); 
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER PROCEDURE [dbo].[PersonListMne]
	                @property int,
	                @od datetime2,
	                @do datetime2
                AS
                BEGIN
	                SELECT 
		                p.ID, 
		                o.Name PropertyName,
		                row_number() over (order by p.checkIn, p.Id) No,
		                p.FirstName, 
		                p.LastName, 
		                p.PersonalNumber, 
		                p.BirthDate,
		                (SELECT cl.Name FROM CodeLists cl WHERE cl.Country = 'MNE' AND cl.ExternalId = p.DocumentType AND cl.Type = 'isprava') DocumentType, 
		                p.DocumentNumber, 
		                (SELECT cl.Name FROM CodeLists cl WHERE cl.Country = 'MNE' AND cl.ExternalId = p.DocumentCountry AND cl.Type = 'drzava') DocumentCountry,		
		                p.CheckIn, 
		                p.CheckOut, 
		                dbo.Nights(p.Id, @do) Nights,
		                COALESCE(p.ResTaxAmount, 0) ResTaxAmount
	                FROM MnePersons p INNER JOIN Properties o ON o.ID = p.PropertyId
	                WHERE o.Id = @property AND CheckIn >= @OD AND CheckIn <= @DO AND p.ExternalId IS NOT NULL
	                order by p.checkIn, p.Id
                END
            ");
        }
    }
}
