using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterTouristOrgLegalEntites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
				ALTER PROCEDURE [dbo].[TouristOrgLegalEntites] 
					@partner int,
					@place nvarchar(256),
					@status nvarchar(256),
					@type nvarchar(256),
					@dateFrom datetime,
					@dateTo datetime
				AS
				BEGIN

					SET NOCOUNT ON;

					select row_number() over (order by Nights DESC, Persons DESC, LegalEntityName) No, * from
					(
						select 
							le.type, le.Name LegalEntityName, le.Address LegalEntityAddress, p.Name PropertyName, p.Address PropertyAddress, 
							p.RegDate, p.RegNumber, cl.ExternalId Place, cl.Name PlaceName, case when p.RegDate > GETDATE() then 1 else 0 end Registered,
							COALESCE(dbo.NightsPerLegalEntityAndPeriod(le.Id, @dateFrom, @dateTo), 0) Nights, 
							dbo.PersonsPerLegalEntityAndPeriod(le.Id, @dateFrom, @dateTo) Persons
						from 
							LegalEntities le inner join Properties p on p.LegalEntityId = le.Id inner join CodeLists cl on cl.ExternalId = p.Place
						where 
							le.PartnerId = @partner
					) a 
					where 
						((a.Registered = 1 AND @status = 'Registered') OR (a.Registered = 0 AND @status = 'Unregistered') OR @status = 'All')
						AND 
						(a.Place = @place OR @place = '') 
						and (a.type = @type OR @type = '')
					order by 
						Nights DESC, Persons DESC, LegalEntityName
				END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
				ALTER PROCEDURE [dbo].[TouristOrgLegalEntites] 
					@partner int,
					@place nvarchar(256),
					@status nvarchar(256),
				--	@type nvarchar(256),
					@dateFrom datetime,
					@dateTo datetime
				AS
				BEGIN

					SET NOCOUNT ON;

					select row_number() over (order by Nights DESC, Persons DESC, LegalEntityName) No, * from
					(
						select 
							le.type, le.Name LegalEntityName, le.Address LegalEntityAddress, p.Name PropertyName, p.Address PropertyAddress, 
							p.RegDate, p.RegNumber, cl.ExternalId Place, cl.Name PlaceName, case when p.RegDate > GETDATE() then 1 else 0 end Registered,
							COALESCE(dbo.NightsPerLegalEntityAndPeriod(le.Id, @dateFrom, @dateTo), 0) Nights, 
							dbo.PersonsPerLegalEntityAndPeriod(le.Id, @dateFrom, @dateTo) Persons
						from 
							LegalEntities le inner join Properties p on p.LegalEntityId = le.Id inner join CodeLists cl on cl.ExternalId = p.Place
						where 
							le.PartnerId = @partner
					) a 
					where 
						((a.Registered = 1 AND @status = 'Registered') OR (a.Registered = 0 AND @status = 'Unregistered') OR @status = 'All')
						AND 
						(a.Place = @place OR @place = '') 
				--		and (a.type = @type OR @type = '')
					order by 
						Nights DESC, Persons DESC, LegalEntityName
				END
            ");
        }
    }
}
