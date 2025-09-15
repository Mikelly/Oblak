using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterTouristOrgResTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
				ALTER PROCEDURE [dbo].[TouristOrgResTax]
					@partnerId int,
					@dateFrom date,
					@dateTo date,
					@group nvarchar(256) = 'CheckInPoint',
					@analitika nvarchar(256)
				AS
				BEGIN
	
					IF @group = 'CheckInPoint'
					BEGIN
						select Datum, Name, SUM(ResTaxAmount) ResTaxAmount, SUM(ResTaxFee) ResTaxFee, SUM(ResTaxAmountUnpaid) ResTaxAmountUnpaid, SUM(ResTaxAmountExternal) ResTaxAmountExternal, SUM(Persons) Persons, 
						COALESCE((SELECT SUM(p.Amount) FROM TaxPayments p INNER JOIN TaxPaymentTypes tp ON tp.Id = p.TaxPaymentTypeId Where CheckInPointId = a.CheckPointId and cast(p.TransactionDate as date) = cast(a.Datum as date) AND tp.IsCash = 1), 0) Payments,
						COALESCE((SELECT COALESCE(SUM(p.Fee), 0) FROM TaxPayments p INNER JOIN TaxPaymentTypes tp ON tp.Id = p.TaxPaymentTypeId Where CheckInPointId = a.CheckPointId and cast(p.TransactionDate as date) = cast(a.Datum as date) AND tp.IsCash = 1), 0) Tax
						from
						(
							select 
								CAST(m.UserCreatedDate as date) Datum
								,cp.Name, cp.Id CheckPointId
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmount
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN m.ResTaxFee ELSE 0 END) ResTaxFee
								,SUM(CASE WHEN rtp.PaymentStatus = 'Unpaid' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmountUnpaid
								,SUM(CASE WHEN rtp.PaymentStatus = 'AlreadyPaid' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmountExternal
								,COUNT(*) Persons
							from MnePersons m inner join Properties p on m.PropertyId = p.Id 
							inner join CheckInPoints cp on cp.Id = m.CheckInPointId 
							inner join LegalEntities le on le.Id = m.LegalEntityId
							inner join ResTaxPaymentTypes rtp on rtp.Id = m.ResTaxPaymentTypeId
							where cast(m.UserCreatedDate as date) between @dateFrom and @dateTo and le.PartnerId = @partnerId and m.GroupId IS NULL and cp.Id > 0
							group by cast(m.UserCreatedDate as date), cp.Name, cp.Id

							union all

							SELECT CAST(g.UserCreatedDate as date) Datum
								,cp.Name, g.CheckInPointId CheckPointId
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmount
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN g.ResTaxFee ELSE 0 END) ResTaxFee
								,SUM(CASE WHEN rtp.PaymentStatus = 'Unpaid' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmountUnpaid
								,SUM(CASE WHEN rtp.PaymentStatus = 'AlreadyPaid' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmountExternal
								,(select count(*) from MnePersons m1 inner join Groups g1 on g1.Id = m1.GroupId where cast(g.UserCreatedDate as date) = cast(g1.UserCreatedDate as date) and g1.CheckInPointId = g.CheckInPointId) Persons
							from Groups g inner join Properties p on g.PropertyId = p.Id 	
							inner join CheckInPoints cp on cp.Id = g.CheckInPointId 
							inner join LegalEntities le on le.Id = g.LegalEntityId
							inner join ResTaxPaymentTypes rtp on rtp.Id = g.ResTaxPaymentTypeId
							where cast(g.UserCreatedDate as date) between @dateFrom and @dateTo and le.PartnerId = @partnerId and cp.Id > 0
							group by cast(g.UserCreatedDate as date), cp.Name, g.CheckInPointId

						) a
						where (@analitika = '-1' or @analitika = Name)
						group by  Datum, Name, CheckPointId
					END
					IF @group = 'Operator'
					BEGIN
						select Datum, Name, SUM(ResTaxAmount) ResTaxAmount, SUM(ResTaxFee) ResTaxFee, SUM(ResTaxAmountUnpaid) ResTaxAmountUnpaid, SUM(ResTaxAmountExternal) ResTaxAmountExternal, SUM(Persons) Persons, 
						COALESCE((SELECT SUM(p.Amount) FROM TaxPayments p INNER JOIN TaxPaymentTypes tp ON tp.Id = p.TaxPaymentTypeId Where p.UserCreated = a.Name and cast(p.TransactionDate as date) = cast(a.Datum as date) AND tp.IsCash = 1), 0) Payments,
						COALESCE((SELECT COALESCE(SUM(p.Fee), 0) FROM TaxPayments p INNER JOIN TaxPaymentTypes tp ON tp.Id = p.TaxPaymentTypeId Where p.UserCreated = a.Name and cast(p.TransactionDate as date) = cast(a.Datum as date) AND tp.IsCash = 1), 0) Tax
		
						from
						(
							select 
								CAST(m.UserCreatedDate as date) Datum
								,m.UserCreated Name
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmount
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN m.ResTaxFee ELSE 0 END) ResTaxFee
								,SUM(CASE WHEN rtp.PaymentStatus = 'Unpaid' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmountUnpaid
								,SUM(CASE WHEN rtp.PaymentStatus = 'AlreadyPaid' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmountExternal
								,COUNT(*) Persons
							from MnePersons m inner join Properties p on m.PropertyId = p.Id 			
							inner join LegalEntities le on le.Id = m.LegalEntityId
							inner join ResTaxPaymentTypes rtp on rtp.Id = m.ResTaxPaymentTypeId
							where cast(m.UserCreatedDate as date) between @dateFrom and @dateTo and le.PartnerId = @partnerId and m.GroupId IS NULL and m.UserCreated <> 'adnan.perazic'
							group by cast(m.UserCreatedDate as date), m.UserCreated

							union all

							SELECT CAST(g.UserCreatedDate as date) Datum
								,g.UserCreated Name
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmount
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN g.ResTaxFee ELSE 0 END) ResTaxFee
								,SUM(CASE WHEN rtp.PaymentStatus = 'Unpaid' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmountUnpaid
								,SUM(CASE WHEN rtp.PaymentStatus = 'AlreadyPaid' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmountExternal
								,(select count(*) from MnePersons m1 inner join Groups g1 on g1.Id = m1.GroupId where cast(g.UserCreatedDate as date) = cast(g1.UserCreatedDate as date) and g1.UserCreated = g.UserCreated) Persons
							from Groups g inner join Properties p on g.PropertyId = p.Id 				
							inner join LegalEntities le on le.Id = g.LegalEntityId
							inner join ResTaxPaymentTypes rtp on rtp.Id = g.ResTaxPaymentTypeId
							where cast(g.UserCreatedDate as date) between @dateFrom and @dateTo and le.PartnerId = @partnerId and g.UserCreated <> 'adnan.perazic'
							group by cast(g.UserCreatedDate as date), g.UserCreated

						) a
						where (@analitika = '-1' or @analitika = Name)
						group by  Datum, Name
					END
					IF @group = 'Place'
					BEGIN
						select Datum, Name, SUM(ResTaxAmount) ResTaxAmount, SUM(ResTaxFee) ResTaxFee, SUM(ResTaxAmountUnpaid) ResTaxAmountUnpaid, SUM(ResTaxAmountExternal) ResTaxAmountExternal, SUM(Persons) Persons, 
						COALESCE((SELECT SUM(p.Amount) FROM TaxPayments p INNER JOIN TaxPaymentTypes tp ON tp.Id = p.TaxPaymentTypeId inner join LegalEntities le1 on le1.Id = p.LegalEntityId inner join Properties p1 on p1.LegalEntityId = le1.Id Where p1.Place = a.ExternalId and cast(p.TransactionDate as date) = cast(a.Datum as date) AND tp.IsCash = 1), 0) Payments,	
						COALESCE((SELECT COALESCE(SUM(p.Fee), 0) FROM TaxPayments p INNER JOIN TaxPaymentTypes tp ON tp.Id = p.TaxPaymentTypeId inner join LegalEntities le1 on le1.Id = p.LegalEntityId inner join Properties p1 on p1.LegalEntityId = le1.Id Where p1.Place = a.ExternalId and cast(p.TransactionDate as date) = cast(a.Datum as date) AND tp.IsCash = 1), 0) Tax	
						from
						(
							select 
								CAST(m.UserCreatedDate as date) Datum
								,cl.Name, cl.ExternalId
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmount
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN m.ResTaxFee ELSE 0 END) ResTaxFee
								,SUM(CASE WHEN rtp.PaymentStatus = 'Unpaid' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmountUnpaid
								,SUM(CASE WHEN rtp.PaymentStatus = 'AlreadyPaid' THEN m.ResTaxAmount ELSE 0 END) ResTaxAmountExternal
								,COUNT(*) Persons
							from MnePersons m inner join Properties p on m.PropertyId = p.Id 		
							inner join CodeLists cl on cl.ExternalId = p.Place and cl.Country = 'MNE' and cl.Type = 'mjesto'
							inner join LegalEntities le on le.Id = m.LegalEntityId
							inner join ResTaxPaymentTypes rtp on rtp.Id = m.ResTaxPaymentTypeId
							where cast(m.UserCreatedDate as date) between @dateFrom and @dateTo and le.PartnerId = @partnerId and m.GroupId IS NULL
							group by cast(m.UserCreatedDate as date), cl.Name, cl.ExternalId

							union all

							SELECT CAST(g.UserCreatedDate as date) Datum
								,cl.Name, cl.ExternalId
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmount
								,SUM(CASE WHEN rtp.PaymentStatus = 'Cash' THEN g.ResTaxFee ELSE 0 END) ResTaxFee
								,SUM(CASE WHEN rtp.PaymentStatus = 'Unpaid' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmountUnpaid
								,SUM(CASE WHEN rtp.PaymentStatus = 'AlreadyPaid' THEN g.ResTaxAmount ELSE 0 END) ResTaxAmountExternal
								,(select count(*) from MnePersons m1 inner join Groups g1 on g1.Id = m1.GroupId inner join Properties p1 on p1.Id = g1.PropertyId where cast(g.UserCreatedDate as date) = cast(g1.UserCreatedDate as date) and p1.Place = cl.ExternalId) Persons
							from Groups g inner join Properties p on g.PropertyId = p.Id 	
							inner join CodeLists cl on cl.ExternalId = p.Place and cl.Country = 'MNE' and cl.Type = 'mjesto'
							inner join LegalEntities le on le.Id = g.LegalEntityId
							inner join ResTaxPaymentTypes rtp on rtp.Id = g.ResTaxPaymentTypeId
							where cast(g.UserCreatedDate as date) between @dateFrom and @dateTo and le.PartnerId = @partnerId 
							group by cast(g.UserCreatedDate as date), cl.Name, cl.ExternalId

						) a
						where (@analitika = '-1' or @analitika = Name)
						group by  Datum, Name, ExternalId
					END
	
				END

            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
