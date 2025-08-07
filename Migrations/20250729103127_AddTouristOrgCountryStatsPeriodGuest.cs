using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AddTouristOrgCountryStatsPeriodGuest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.TouristOrgCountryStatsPeriodGuest', 'FN') IS NOT NULL
                    DROP FUNCTION dbo.TouristOrgCountryStatsPeriodGuest;
            ");

            migrationBuilder.Sql(@"
            CREATE PROCEDURE [dbo].[TouristOrgCountryStatsPeriodGuest]
				@partnerid int,
				@datefrom datetime,
				@dateto datetime	
			AS
			BEGIN
	
				SET NOCOUNT ON;

				SELECT CountryDesc, SUM(Guest) Guests, SUM(Nights) Nights, SUM(ResTaxAmount) ResTax, 
				ROUND(CAST(SUM(Guest) as decimal(10,2)) / CAST(TotalGuests as decimal(10,2)), 4) * 100 CountryGuestShare, 
				ROUND(CAST(SUM(Nights) as decimal(10,2)) / CAST(TotalNights as decimal(10,2)), 4) * 100 CountryNightsShare, 
				TotalGuests, TotalNights 
				FROM
				(
					SELECT 	
						m.DocumentCountry Country,	
						(select cl.Name from CodeLists cl where cl.Country = 'MNE' and cl.Type = 'drzava' and cl.ExternalId = m.DocumentCountry) CountryDesc,		
						1 Guest,
						COUNT(*) OVER (PARTITION BY 1) TotalGuests,
						datediff(day, case when @datefrom > cast(checkin as date) then @datefrom else cast(checkin as date) end, case when @dateto < cast(coalesce(checkout, @dateto) as date) then @dateto else cast(coalesce(checkout, @dateto) as date) end) + 1 Nights,
						SUM(datediff(day, case when @datefrom > cast(checkin as date) then @datefrom else cast(checkin as date) end, case when @dateto < cast(coalesce(checkout, @dateto) as date) then @dateto else cast(coalesce(checkout, @dateto) as date) end) + 1) OVER (PARTITION BY 1) TotalNights,
						m.ResTaxAmount			
					FROM MnePersons m 
						INNER JOIN Properties p on m.PropertyId = p.Id 
						INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
						LEFT JOIN ResTaxTypes rt ON rt.Id = m.ResTaxTypeId
						LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = m.ResTaxPaymentTypeId
					WHERE 
						le.PartnerId = @partnerid AND 
						--m.UserCreated = @user AND		
						cast(m.UserCreatedDate as date) between @dateFrom and @dateTo
				) a
				GROUP BY CountryDesc, TotalGuests, TotalNights
				ORDER BY 3 DESC

			END

            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('dbo.TouristOrgCountryStatsPeriodGuest', 'FN') IS NOT NULL
                    DROP FUNCTION dbo.TouristOrgCountryStatsPeriodGuest;
            ");
        }    }
}
