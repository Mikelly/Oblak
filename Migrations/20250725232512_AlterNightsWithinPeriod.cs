using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterNightsWithinPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER FUNCTION [dbo].[TotalNightsWithinPeriod]
                (
                    @personId INT,
                    @from     DATE,
                    @to       DATE
                )
                RETURNS INT
                AS
                BEGIN
                    DECLARE @nights INT = 0;

                    SELECT @nights = SUM(
                        CASE 
                            WHEN CheckOut IS NULL THEN 0
                            ELSE 
                                CASE 
                                    WHEN DATEDIFF(DAY, 
                                        CASE WHEN CheckIn < @from THEN @from ELSE CheckIn END,
                                        CASE WHEN CheckOut > @to   THEN @to   ELSE CheckOut END
                                    ) < 0 THEN 0
                                    ELSE DATEDIFF(DAY, 
                                        CASE WHEN CheckIn < @from THEN @from ELSE CheckIn END,
                                        CASE WHEN CheckOut > @to   THEN @to   ELSE CheckOut END
                                    )
                                END
                        END
                    )
                    FROM MnePersons
                    WHERE Id          = @personId
                      AND CheckOut   IS NOT NULL
                      AND CheckIn    <  @to 
                      AND CheckOut   >  @from;

                    RETURN ISNULL(@nights, 0);
                END;
                "); 
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
