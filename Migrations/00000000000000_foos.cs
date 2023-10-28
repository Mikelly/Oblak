using System;
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
                CREATE FUNCTION [dbo].[rb90GroupDesc]
                (
	                @id int
                )
                RETURNS nvarchar(2000)
                AS
                BEGIN
	                DECLARE @retval nvarchar(256)
	                SELECT @retval = o.Naziv + COALESCE(' - ' + j.Naziv, '') FROM rb_Grupe g INNER JOIN rb_Objekti o ON o.ID = g.ObjekatID LEFT JOIN rb_Jedinice j ON j.ID = g.JedinicaID
	                WHERE g.ID = @id	
	                RETURN @retval
                END
                ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[rb90GuestList]
                (
	                @id int
                )
                RETURNS nvarchar(2000)
                AS
                BEGIN
	                DECLARE @retval nvarchar(256)	
	                SET @retval =
	                (
		                SELECT STRING_AGG(COALESCE(p.Ime + ' ', '') + COALESCE(p.Prezime, '') + ' (' + p.LD_Drzava + ')', ', ') FROM rb_Grupe g INNER JOIN rb_Prijave p ON p.GrupaID = g.ID
		                WHERE g.ID = @id
	                )	
	                RETURN CAST((SELECT COUNT(*) FROM rb_Prijave p WHERE p.GrupaID = @id) as nvarchar(50)) + ': ' + @retval
                END
                ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[rb90Nights]
                (
	                @id int,	                
	                @last datetime
                )
                RETURNS int
                AS
                BEGIN	
	                DECLARE @nights int
	                SELECT @nights = DATEDIFF(day, Borav_Prijava, CASE WHEN COALESCE(Borav_Odjava, @last) > @last THEN @last ELSE Borav_Odjava END) FROM rb_Prijave WHERE ID = @id	                
	                RETURN COALESCE(ABS(@nights), 0)
                END
                ");

            migrationBuilder.Sql(@"
                CREATE FUNCTION [dbo].[rb90PropertyDesc]
                (
	                @id int
                )
                RETURNS nvarchar(2000)
                AS
                BEGIN
	                DECLARE @retval nvarchar(256)
	                DECLARE @grupa int
	                SELECT @grupa = GrupaID FROM rb_Prijave WHERE ID = @id

	                IF @grupa IS NULL
	                BEGIN
		                SELECT @retval = o.Naziv + COALESCE(' - ' + j.Naziv, '') FROM rb_Grupe g INNER JOIN rb_Objekti o ON o.ID = g.ObjekatID LEFT JOIN rb_Jedinice j ON j.ID = g.JedinicaID
		                WHERE g.ID = @grupa
	                END
	                ELSE
	                BEGIN
		                SELECT @retval = o.Naziv FROM rb_Prijave p INNER JOIN rb_Objekti o ON o.RefID = p.ObjekatID
		                WHERE p.ID = @id
	                END
		
	                RETURN @retval
                END
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
