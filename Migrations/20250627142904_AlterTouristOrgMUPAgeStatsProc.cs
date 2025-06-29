using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterTouristOrgMUPAgeStatsProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql(@"
			ALTER PROCEDURE [dbo].[TouristOrgMUPAgeStats]
				@partnerid int,	
				@LegalEntityname nvarchar(250),
				@dateFrom datetime,
				@dateTo datetime
			AS
			BEGIN
	
				SET NOCOUNT ON;

				declare @lename nvarchar(250) = '%' + @LegalEntityname + '%'

				SELECT LegalEntityName, [Address], sum(Nights1) TotalNights1, sum(Nights2) TotalNights2, sum(Nights3) TotalNights3,
				sum(Guests) TotalGuests
				FROM
				(
					SELECT 	
						LegalEntityName, [Address],	
			
						iif (datediff(year, dateofBirth, getdate()) < 12, SUM(DATEDIFF(day, 
							CASE WHEN CheckIn >= @dateFrom THEN CheckIn ELSE @dateFrom END,
							CASE WHEN CheckOut >= @dateTo THEN @dateTo ELSE CheckOut END
							) + 1) , 0) Nights1, 
						iif (datediff(year, dateofBirth, getdate()) >= 12 and datediff(year, dateofBirth, getdate()) <= 18, 
						SUM(DATEDIFF(day, 
							CASE WHEN CheckIn >= @dateFrom THEN CheckIn ELSE @dateFrom END,
							CASE WHEN CheckOut >= @dateTo THEN @dateTo ELSE CheckOut END
							) + 1), 0) Nights2, 
						iif (datediff(year, dateofBirth, getdate()) > 18, SUM(DATEDIFF(day, 
							CASE WHEN CheckIn >= @dateFrom THEN CheckIn ELSE @dateFrom END,
							CASE WHEN CheckOut >= @dateTo THEN @dateTo ELSE CheckOut END
							) + 1) , 0) Nights3,
						COUNT(*) Guests			
					FROM MneMupData  			
					WHERE 
						(LegalEntityName like @lename or @LegalEntityname = '-1') and
						(CAST(CheckIn as date) < @dateTo) and
						(CAST(CheckOut as date) > @dateFrom )
					group by LegalEntityName, [Address], datediff(year, dateofBirth, getdate())
				) a
				GROUP BY LegalEntityName, [Address]
				ORDER BY LegalEntityName, [Address]

			END
			");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"

            ALTER PROCEDURE [dbo].[TouristOrgMUPCountryStats]
				@partnerid int,	
				@dateFrom datetime,
				@dateTo datetime
			AS
			BEGIN
	
				SET NOCOUNT ON;

				SELECT CountryDesc,  
				ROUND(100 * cast(TotalNights as decimal(10,2)) /
							cast((select SUM(DATEDIFF(day, 
							CASE WHEN CheckIn >= @dateFrom THEN CheckIn ELSE @dateFrom END,
							CASE WHEN CheckOut >= @dateTo THEN @dateTo ELSE CheckOut END
							) + 1) 		
							FROM MneMupData m 	
							WHERE 
							(CAST(CheckIn as date) < @dateTo) and
							(CAST(CheckOut as date) > @dateFrom )) as decimal(10,2)) ,2)	TotalNightsPercent, 	

				TotalGuests, TotalNights 
				FROM
				(
					SELECT 	
						m.DocumentCountry CountryDesc,	
						COUNT(*) TotalGuests,		
						SUM(DATEDIFF(day, 
							CASE WHEN CheckIn >= @dateFrom THEN CheckIn ELSE @dateFrom END,
							CASE WHEN CheckOut >= @dateTo THEN @dateTo ELSE CheckOut END
							) + 1) TotalNights		
					FROM MneMupData m 			
					WHERE 
	
						(CAST(CheckIn as date) < @dateTo) and
						(CAST(CheckOut as date) > @dateFrom )
					group by m.DocumentCountry
				) a
			--	GROUP BY CountryDesc, TotalGuests, TotalNights
				ORDER BY TotalGuests DESC

			END 
			");
        }
    }
}
