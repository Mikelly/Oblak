using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterTouristOrgDebtProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER PROCEDURE [dbo].[TouristOrgDebt]
                	 @partner int = 4 
                AS
                BEGIN


                	select le.[name] Izdavaoc, p.[name] Apartman, p.[address] Adresa, cl.[name] Mjesto, b.Saldo, le.PhoneNumber from

                	(

                	select Id, SUM(Duguje - Potrazuje) Saldo

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
                			where le.PartnerId  = @partner and m.GroupId is null and m.ResTaxAmount > 0 and CAST(m.UserCreatedDate as date) < getdate()-30 ) aa
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
                			where le.PartnerId  = @partner and g.ResTaxAmount > 0 and CAST(g.UserCreatedDate as date) < getdate()-30 ) bb
                			group by Id

                			union all

                			select pay.LegalEntityId Id, 0 duguje, sum(pay.Amount) potrazuje
                			from TaxPayments pay inner join LegalEntities le on le.Id = pay.LegalEntityId inner join TaxPaymentTypes t on t.Id = pay.TaxPaymentTypeId
                			where pay.PersonId is null and pay.GroupId is null 
                			group by  pay.LegalEntityId

                		) a
                		group by Id
                	) b 
                	inner join LegalEntities le on le.Id = b.Id
                	inner join Properties p on p.LegalEntityId = b.Id 
                	inner join CodeLists cl on cl.ExternalId = p.Place
                	where saldo > 0 order by b.saldo desc


                END
                
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
