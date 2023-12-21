using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class foos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[GroupDesc]
                (
                    @id int
                )
                RETURNS nvarchar(2000)
                AS
                BEGIN
                    DECLARE @retval nvarchar(256)
                    SELECT @retval = p.Name + COALESCE(' - ' + u.Name, '') FROM Groups g INNER JOIN Properties p ON p.Id = g.PropertyId LEFT JOIN PropertyUnits u ON u.Id = g.UnitId
                    WHERE g.ID = @id
                    RETURN @retval
                END
                ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[GuestList]
                (
                    @id int
                )
                RETURNS nvarchar(2000)
                AS
                BEGIN
                    DECLARE @retval nvarchar(256)	
                    DECLARE @cntry nvarchar(256) = (SELECT TOP 1 Country FROM Groups g INNER JOIN Properties p ON g.PropertyId = p.Id INNER JOIN LegalEntities l ON l.Id = p.LegalEntityId)
	
	                IF @cntry = 'SRB'
	                BEGIN
		                SET @retval =
		                (
			                SELECT STRING_AGG(COALESCE(p.FirstName + ' ', '') + COALESCE(p.LastName, '') + ' (' + COALESCE(p.NationalityIso3, 'SRB') + ')', ', ') FROM Groups g INNER JOIN SrbPersons p ON p.GroupId = g.ID
			                WHERE g.ID = @id
		                )	
		                SET @retval = CAST((SELECT COUNT(*) FROM SrbPersons p WHERE p.GroupId = @id) as nvarchar(50)) + ': ' + @retval
	                END

	                IF @cntry = 'MNE'
	                BEGIN
		                SET @retval =
		                (
			                SELECT STRING_AGG(COALESCE(p.FirstName + ' ', '') + COALESCE(p.LastName, '') + ' (' + p.Nationality + ')', ', ') FROM Groups g INNER JOIN MnePersons p ON p.GroupId = g.ID
			                WHERE g.ID = @id
		                )	
		                SET @retval = CAST((SELECT COUNT(*) FROM MnePersons p WHERE p.GroupId = @id) as nvarchar(50)) + ': ' + @retval
	                END

	                RETURN @retval
                END
                ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[Nights]
                (
                    @id int,	                
                    @last datetime
                )
                RETURNS int
                AS
                BEGIN	
                    DECLARE @nights int
                    SELECT @nights = DATEDIFF(day, CheckIn, CASE WHEN COALESCE(CheckOut, @last) > @last THEN @last ELSE CheckOut END) FROM SrbPersons WHERE Id = @id	                
                    RETURN COALESCE(ABS(@nights), 0)
                END
                ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[PropertyDesc]
                (
                    @id int
                )
                RETURNS nvarchar(2000)
                AS
                BEGIN
                    DECLARE @retval nvarchar(256)
                    DECLARE @grupa int
                    SELECT @grupa = GroupId FROM SrbPersons WHERE Id = @id

                    IF @grupa IS NULL
                    BEGIN
                        SELECT @retval = p.Name + COALESCE(' - ' + u.Name, '') FROM Groups g INNER JOIN Properties p ON p.Id = g.PropertyId LEFT JOIN PropertyUnits u ON u.Id = g.UnitId
                        WHERE g.ID = @grupa
                    END
                    ELSE
                    BEGIN
                        SELECT @retval = pr.Name FROM SrbPersons p INNER JOIN Properties pr ON p.GroupId = pr.Id
                        WHERE p.ID = @id
                    END
		
                    RETURN @retval
                END
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP FUNCTION [dbo].[GroupDesc]");

            migrationBuilder.Sql(@"DROP FUNCTION [dbo].[GuestList]");

            migrationBuilder.Sql(@"DROP FUNCTION [dbo].[Nights]");

            migrationBuilder.Sql(@"DROP FUNCTION [dbo].[PropertyDesc]");
        }
    }
}
