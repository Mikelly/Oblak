using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterTouristOrgLegalEntities2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
GO

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
							le.id LegalEntity, le.type, le.Name LegalEntityName, le.Address LegalEntityAddress, p.Name PropertyName, p.Address PropertyAddress, 
							p.RegDate, p.RegNumber, cl.ExternalId Place, cl.Name PlaceName, case when p.RegDate > GETDATE() then 1 else 0 end Registered,
							COALESCE(dbo.NightsPerLegalEntityAndPeriod(le.Id, @dateFrom, @dateTo), 0) Nights, 
							dbo.PersonsPerLegalEntityAndPeriod(le.Id, @dateFrom, @dateTo) Persons
						from 
							LegalEntities le inner join Properties p on p.LegalEntityId = le.Id inner join CodeLists cl on cl.ExternalId = p.Place
						where 
							le.PartnerId = @partner
					) a 

				-- Uplata za trazeni period
					left outer join
					(			
							select Id LegalEntity, sum(duguje) Duguje, sum(potrazuje) Potrazuje 
							from 
							(
								select le.Id, m.ResTaxAmount Duguje, 
								CASE 
									WHEN rp.PaymentStatus = 'Cash' THEN m.ResTaxAmount 
									WHEN rp.PaymentStatus = 'AlreadyPaid' THEN (SELECT pay.Amount FROM TaxPayments pay WHERE pay.PersonId = m.Id)
									WHEN rp.PaymentStatus = 'Unpaid' THEN 0
								END Potrazuje 
							from MnePersons m inner join Properties p on p.Id = m.PropertyId inner join LegalEntities le on le.Id = p.LegalEntityId inner join ResTaxPaymentTypes rp on rp.Id = m.ResTaxPaymentTypeId
							where le.PartnerId  = @partner and m.GroupId is null and m.ResTaxAmount > 0 and CAST(m.UserCreatedDate as date) between @dateFrom and @dateTo
							) a1 group by Id

							union all
			
							select Id, sum(duguje) Duguje, sum(potrazuje) Potrazuje from (
							select le.Id,
								g.ResTaxAmount Duguje, 
								CASE 
									WHEN rp.PaymentStatus = 'Cash' THEN g.ResTaxAmount 
									WHEN rp.PaymentStatus = 'AlreadyPaid' THEN (SELECT pay.Amount FROM TaxPayments pay WHERE pay.GroupId = g.Id)
									WHEN rp.PaymentStatus = 'Unpaid' THEN 0
								END Potrazuje 
							from Groups g inner join Properties p on p.Id = g.PropertyId inner join LegalEntities le on le.Id = p.LegalEntityId inner join ResTaxPaymentTypes rp on rp.Id = g.ResTaxPaymentTypeId
							where le.PartnerId  = @partner and g.ResTaxAmount > 0  and CAST(g.UserCreatedDate as date) between @dateFrom and @dateTo
							) a2 group by Id

							union all

							select pay.LegalEntityId, 0 duguje, sum(pay.Amount) potrazuje
							from TaxPayments pay inner join LegalEntities le on le.Id = pay.LegalEntityId inner join TaxPaymentTypes t on t.Id = pay.TaxPaymentTypeId
							where pay.PersonId is null and pay.GroupId is null and CAST(pay.TransactionDate as date) between @dateFrom and @dateTo
							group by pay.LegalEntityId

	
					) b on a.LegalEntity = b.LegalEntity

				-- ukupni saldo
					left outer join
					(
						select Id LegalEntity, SUM(Duguje - Potrazuje) Saldo
						from
						(
							select Id, sum(duguje) duguje, sum(potrazuje) potrazuje from 
							(select le.Id, m.ResTaxAmount Duguje, 
								CASE 
									WHEN rp.PaymentStatus = 'Cash' THEN m.ResTaxAmount 
									WHEN rp.PaymentStatus = 'AlreadyPaid' THEN (SELECT pay.Amount FROM TaxPayments pay WHERE pay.PersonId = m.Id)
									WHEN rp.PaymentStatus = 'Unpaid' THEN 0
								END Potrazuje 
							from MnePersons m inner join Properties p on p.Id = m.PropertyId inner join LegalEntities le on le.Id = p.LegalEntityId inner join ResTaxPaymentTypes rp on rp.Id = m.ResTaxPaymentTypeId
							where le.PartnerId  = @partner and m.GroupId is null and m.ResTaxAmount > 0 ) aa
							group by Id
							union all
							select Id, sum(duguje) duguje, sum(potrazuje) potrazuje from 
							(select le.Id, 
								g.ResTaxAmount Duguje, 
								CASE 
									WHEN rp.PaymentStatus = 'Cash' THEN g.ResTaxAmount 
									WHEN rp.PaymentStatus = 'AlreadyPaid' THEN (SELECT pay.Amount FROM TaxPayments pay WHERE pay.GroupId = g.Id)
									WHEN rp.PaymentStatus = 'Unpaid' THEN 0
								END Potrazuje 
							from Groups g inner join Properties p on p.Id = g.PropertyId inner join LegalEntities le on le.Id = p.LegalEntityId inner join ResTaxPaymentTypes rp on rp.Id = g.ResTaxPaymentTypeId
							where le.PartnerId  = @partner and g.ResTaxAmount > 0 ) bb
							group by Id
							union all
							select pay.LegalEntityId Id, 0 duguje, sum(pay.Amount) potrazuje
							from TaxPayments pay inner join LegalEntities le on le.Id = pay.LegalEntityId inner join TaxPaymentTypes t on t.Id = pay.TaxPaymentTypeId
							where pay.PersonId is null and pay.GroupId is null 
							group by  pay.LegalEntityId
						) a
						group by Id
					) c on a.LegalEntity = c.LegalEntity

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

        }
    }
}
