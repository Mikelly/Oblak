using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AlterTouristOrgPostOffice2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
			ALTER PROCEDURE [dbo].[TouristOrgPostOffice]
				@date datetime,
				@partnerid int,
				@checkinpoint int,
				@taxType nvarchar(256),
				@id int,
				@g int,
				@inv int,
				@pay int
			AS
			BEGIN
				IF @taxType = 'ResidenceTax'
				BEGIN
					SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY UserCreatedDate), '00000') OrderNo, * 
					FROM
					(
						SELECT 				
							s.PaymentAccount Account,
							s.PaymentDescription Description,
							s.PaymentName RecipientName,
							le.Name PayeeName,
							le.Address PayeeAddress,
							'' PayRef,
							le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) TIN,		
							'Gost: ' + m.FirstName + ' ' + m.LastName + char(13) + char(10) + 'Period: ' + FORMAT(m.CheckIn, 'dd.MM.yyyy') + ' - ' + COALESCE(FORMAT(m.CheckOut, 'dd.MM.yyyy'), '') Guest,
							COALESCE(m.ResTaxAmount, 0) Tax,
							COALESCE(m.ResTaxFee, 0) Fee,
							m.UserCreated,
							m.UserCreatedDate,
							cp.Name CheckInPointName
						FROM MnePersons m 
							INNER JOIN Properties p on m.PropertyId = p.Id 
							INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
							INNER JOIN Partners pn ON pn.Id = le.PartnerId
							LEFT JOIN ResTaxTypes rt ON rt.Id = m.ResTaxTypeId
							LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = m.ResTaxPaymentTypeId	
							LEFT JOIN CheckInPoints cp ON cp.Id = m.CheckInPointId
							LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						WHERE 
							m.Id = @id 
							OR
							(			
								@id = 0
								AND m.GroupId IS NULL
								AND rtp.PaymentStatus = 'Cash' 
								AND COALESCE(m.ResTaxAmount, 0) + COALESCE(m.ResTaxFee, 0) > 0
								AND le.PartnerId = @partnerid 
								AND m.CheckInPointId = @checkinpoint
								AND CAST(m.UserCreatedDate as date) = CAST(@date as date)
							)
						UNION ALL
						SELECT 				
							s.PaymentAccount Account,
							s.PaymentDescription Description,
							s.PaymentName RecipientName,
							le.Name PayeeName,
							le.Address PayeeAddress,
							'' PayRef,
							le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) TIN,		
							'' Guest,
							COALESCE(g.ResTaxAmount, 0) Tax,
							COALESCE(g.ResTaxFee, 0) Fee,
							g.UserCreated,
							g.UserCreatedDate,
							cp.Name CheckInPointName
						FROM Groups g 
							INNER JOIN Properties p on g.PropertyId = p.Id 
							INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
							INNER JOIN Partners pn ON pn.Id = le.PartnerId			
							LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = g.ResTaxPaymentTypeId	
							LEFT JOIN CheckInPoints cp ON cp.Id = g.CheckInPointId
							LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						WHERE 
							g.Id = @g
							OR
							(		
								g.VesselId IS NULL					
								AND rtp.PaymentStatus = 'Cash' 
								AND COALESCE(g.ResTaxAmount, 0) + COALESCE(g.ResTaxFee, 0) > 0
								AND le.PartnerId = @partnerid 
								AND g.CheckInPointId = @checkinpoint
								AND CAST(g.UserCreatedDate as date) = CAST(@date as date)
							)
						UNION ALL
						SELECT 				
							s.PaymentAccount Account,
							s.PaymentDescription Description,
							s.PaymentName RecipientName,
							le.Name PayeeName,
							le.Address PayeeAddress,
							'' PayRef,
							le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) TIN,		
							'' Guest,
							COALESCE(p.Amount, 0) Tax,
							COALESCE(p.Fee, 0) Fee,
							p.UserCreated,
							p.UserCreatedDate,
							cp.Name CheckInPointName
						FROM TaxPayments p 				
							INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
							INNER JOIN Partners pn ON pn.Id = le.PartnerId			
							INNER JOIN TaxPaymentTypes tpt ON tpt.Id = p.TaxPaymentTypeId
							LEFT JOIN CheckInPoints cp ON cp.Id = p.CheckInPointId
							LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						WHERE 
							p.Id = @pay
							OR
							(	
								tpt.TaxPaymentStatus = 'Cash' 
								AND COALESCE(p.Amount, 0) > 0
								AND le.PartnerId = @partnerid 
								AND p.CheckInPointId = @checkinpoint
								AND CAST(p.UserCreatedDate as date) = CAST(@date as date)
							)
					) a		
				END
				IF @taxType = 'NauticalTax'
				BEGIN
					SELECT 		
						FORMAT(ROW_NUMBER() OVER (ORDER BY g.UserCreatedDate), '00000') OrderNo,
						s.PaymentAccount AS Account,
						FORMAT(g.CheckIn, 'dd.MM.yyyy') + ' - ' + FORMAT(g.CheckOut, 'dd.MM.yyyy') AS Description,
						s.PaymentName AS RecipientName,
						v.OwnerName AS PayeeName,
						c.CountryName AS PayeeAddress,
						'' AS PayRef,		
						ISNULL(v.OwnerTIN, le.TIN) + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) AS TIN, 
						'' AS Guest,
						g.ResTaxAmount AS Tax,
						g.ResTaxFee AS Fee,
						g.UserCreated,
						g.UserCreatedDate,
						cp.Name AS CheckInPointName
					FROM Groups g 
						INNER JOIN Properties p ON g.PropertyId = p.Id 
						INNER JOIN LegalEntities le ON le.Id = p.LegalEntityId
						INNER JOIN Partners pn ON pn.Id = le.PartnerId			
						LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = g.ResTaxPaymentTypeId	
						LEFT JOIN CheckInPoints cp ON cp.Id = g.CheckInPointId
						LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						LEFT JOIN Vessels v ON v.Id = g.VesselId
						LEFT JOIN Countries c ON c.Id = v.CountryId
					WHERE 
						g.Id = @g 
						OR
						(		
							g.VesselId IS NOT NULL
							AND rtp.PaymentStatus = 'Cash' 
							AND COALESCE(g.ResTaxAmount, 0) + COALESCE(g.ResTaxFee, 0) > 0 
							AND le.PartnerId = @partnerid 
							AND g.CheckInPointId = @checkinpoint
							AND CAST(g.UserCreatedDate as date) = CAST(@date as date)
						)
				END 
				IF @taxType = 'ExcursionTax'
				BEGIN
					SELECT 
						FORMAT(ROW_NUMBER() OVER (ORDER BY i.UserCreatedDate), '00000') OrderNo,
						s.PaymentAccount Account,
						s.PaymentDescription Description,
						pn.Name RecipientName,
						ag.Name PayeeName, 
						CASE 
							WHEN ag.Address IS NULL THEN ''
							ELSE ag.Address
						END AS PayeeAddress,
						i.InvoiceNumber PayRef,
						CASE 
							WHEN ag.TIN IS NULL THEN ''
							ELSE ag.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END)
						END AS TIN,		
						'' Guest,
						i.BillingAmount Tax,
						i.BillingFee Fee,
						ISNULL(i.UserCreated, '') as UserCreated,
						ISNULL(i.UserCreatedDate, '') as UserCreatedDate, 
						cp.Name CheckInPointName
					FROM ExcursionInvoices i
						INNER JOIN Agencies ag ON ag.Id = i.AgencyId
						INNER JOIN Partners pn ON pn.Id = ag.PartnerId
						INNER JOIN TaxPaymentTypes tpt ON tpt.Id = i.TaxPaymentTypeId
						LEFT JOIN CheckInPoints cp ON cp.Id = i.CheckInPointId
						LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
					WHERE 			
						i.Id = @inv 
						OR
						(
							tpt.IsCash = 1
							AND COALESCE(i.BillingAmount, 0) + COALESCE(i.BillingFee, 0) > 0 
							AND pn.Id = @partnerid 
							AND i.CheckInPointId = @checkinpoint
							AND CAST(i.UserCreatedDate as date) = CAST(@date as date)
						)
				END
			END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
			ALTER PROCEDURE [dbo].[TouristOrgPostOffice]
				@date datetime,
				@partnerid int,
				@checkinpoint int,
				@taxType nvarchar(256),
				@id int,
				@g int,
				@inv int,
				@pay int
			AS
			BEGIN
				IF @taxType = 'ResidenceTax'
				BEGIN
					SELECT FORMAT(ROW_NUMBER() OVER (ORDER BY UserCreatedDate), '00000') OrderNo, * 
					FROM
					(
						SELECT 				
							s.PaymentAccount Account,
							s.PaymentDescription Description,
							s.PaymentName RecipientName,
							le.Name PayeeName,
							le.Address PayeeAddress,
							'' PayRef,
							le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) TIN,		
							'Gost: ' + m.FirstName + ' ' + m.LastName + char(13) + char(10) + 'Period: ' + FORMAT(m.CheckIn, 'dd.MM.yyyy') + ' - ' + COALESCE(FORMAT(m.CheckOut, 'dd.MM.yyyy'), '') Guest,
							COALESCE(m.ResTaxAmount, 0) Tax,
							COALESCE(m.ResTaxFee, 0) Fee,
							m.UserCreated,
							m.UserCreatedDate,
							cp.Name CheckInPointName
						FROM MnePersons m 
							INNER JOIN Properties p on m.PropertyId = p.Id 
							INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
							INNER JOIN Partners pn ON pn.Id = le.PartnerId
							LEFT JOIN ResTaxTypes rt ON rt.Id = m.ResTaxTypeId
							LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = m.ResTaxPaymentTypeId	
							LEFT JOIN CheckInPoints cp ON cp.Id = m.CheckInPointId
							LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						WHERE 
							m.Id = @id 
							OR
							(			
								@id = 0
								AND m.GroupId IS NULL
								AND rtp.PaymentStatus = 'Cash' 
								AND COALESCE(m.ResTaxAmount, 0) + COALESCE(m.ResTaxFee, 0) > 0
								AND le.PartnerId = @partnerid 
								AND m.CheckInPointId = @checkinpoint
								AND CAST(m.UserCreatedDate as date) = CAST(@date as date)
							)
						UNION ALL
						SELECT 				
							s.PaymentAccount Account,
							s.PaymentDescription Description,
							s.PaymentName RecipientName,
							le.Name PayeeName,
							le.Address PayeeAddress,
							'' PayRef,
							le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) TIN,		
							'' Guest,
							COALESCE(g.ResTaxAmount, 0) Tax,
							COALESCE(g.ResTaxFee, 0) Fee,
							g.UserCreated,
							g.UserCreatedDate,
							cp.Name CheckInPointName
						FROM Groups g 
							INNER JOIN Properties p on g.PropertyId = p.Id 
							INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
							INNER JOIN Partners pn ON pn.Id = le.PartnerId			
							LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = g.ResTaxPaymentTypeId	
							LEFT JOIN CheckInPoints cp ON cp.Id = g.CheckInPointId
							LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						WHERE 
							g.Id = @g
							OR
							(		
								g.VesselId IS NULL					
								AND rtp.PaymentStatus = 'Cash' 
								AND COALESCE(g.ResTaxAmount, 0) + COALESCE(g.ResTaxFee, 0) > 0
								AND le.PartnerId = @partnerid 
								AND g.CheckInPointId = @checkinpoint
								AND CAST(g.UserCreatedDate as date) = CAST(@date as date)
							)
						UNION ALL
						SELECT 				
							s.PaymentAccount Account,
							s.PaymentDescription Description,
							s.PaymentName RecipientName,
							le.Name PayeeName,
							le.Address PayeeAddress,
							'' PayRef,
							le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) TIN,		
							'' Guest,
							COALESCE(p.Amount, 0) Tax,
							COALESCE(p.Fee, 0) Fee,
							p.UserCreated,
							p.UserCreatedDate,
							cp.Name CheckInPointName
						FROM TaxPayments p 				
							INNER JOIN LegalEntities le on le.Id = p.LegalEntityId
							INNER JOIN Partners pn ON pn.Id = le.PartnerId			
							INNER JOIN TaxPaymentTypes tpt ON tpt.Id = p.TaxPaymentTypeId
							LEFT JOIN CheckInPoints cp ON cp.Id = p.CheckInPointId
							LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						WHERE 
							p.Id = @pay
							OR
							(	
								tpt.TaxPaymentStatus = 'Cash' 
								AND COALESCE(p.Amount, 0) > 0
								AND le.PartnerId = @partnerid 
								AND p.CheckInPointId = @checkinpoint
								AND CAST(p.UserCreatedDate as date) = CAST(@date as date)
							)
					) a		
				END
				IF @taxType = 'NauticalTax'
				BEGIN
					SELECT 		
						FORMAT(ROW_NUMBER() OVER (ORDER BY g.UserCreatedDate), '00000') OrderNo,
						s.PaymentAccount AS Account,
						FORMAT(g.CheckIn, 'dd.MM.yyyy') + ' - ' + FORMAT(g.CheckOut, 'dd.MM.yyyy') AS Description,
						s.PaymentName AS RecipientName,
						v.OwnerName AS PayeeName,
						c.CountryName AS PayeeAddress,
						'' AS PayRef,
						le.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END) AS TIN,		
						'' AS Guest,
						g.ResTaxAmount AS Tax,
						g.ResTaxFee AS Fee,
						g.UserCreated,
						g.UserCreatedDate,
						cp.Name AS CheckInPointName
					FROM Groups g 
						INNER JOIN Properties p ON g.PropertyId = p.Id 
						INNER JOIN LegalEntities le ON le.Id = p.LegalEntityId
						INNER JOIN Partners pn ON pn.Id = le.PartnerId			
						LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = g.ResTaxPaymentTypeId	
						LEFT JOIN CheckInPoints cp ON cp.Id = g.CheckInPointId
						LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
						LEFT JOIN Vessels v ON v.Id = g.VesselId
						LEFT JOIN Countries c ON c.Id = v.CountryId
					WHERE 
						g.Id = @g 
						OR
						(		
							g.VesselId IS NOT NULL
							AND rtp.PaymentStatus = 'Cash' 
							AND COALESCE(g.ResTaxAmount, 0) + COALESCE(g.ResTaxFee, 0) > 0 
							AND le.PartnerId = @partnerid 
							AND g.CheckInPointId = @checkinpoint
							AND CAST(g.UserCreatedDate as date) = CAST(@date as date)
						)
				END 
				IF @taxType = 'ExcursionTax'
				BEGIN
					SELECT 
						FORMAT(ROW_NUMBER() OVER (ORDER BY i.UserCreatedDate), '00000') OrderNo,
						s.PaymentAccount Account,
						s.PaymentDescription Description,
						pn.Name RecipientName,
						ag.Name PayeeName, 
						CASE 
							WHEN ag.Address IS NULL THEN ''
							ELSE ag.Address
						END AS PayeeAddress,
						i.InvoiceNumber PayRef,
						CASE 
							WHEN ag.TIN IS NULL THEN ''
							ELSE ag.TIN + (CASE WHEN @partnerid = 4 THEN '-817' ELSE '' END)
						END AS TIN,		
						'' Guest,
						i.BillingAmount Tax,
						i.BillingFee Fee,
						ISNULL(i.UserCreated, '') as UserCreated,
						ISNULL(i.UserCreatedDate, '') as UserCreatedDate, 
						cp.Name CheckInPointName
					FROM ExcursionInvoices i
						INNER JOIN Agencies ag ON ag.Id = i.AgencyId
						INNER JOIN Partners pn ON pn.Id = ag.PartnerId
						INNER JOIN TaxPaymentTypes tpt ON tpt.Id = i.TaxPaymentTypeId
						LEFT JOIN CheckInPoints cp ON cp.Id = i.CheckInPointId
						LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = @taxType
					WHERE 			
						i.Id = @inv 
						OR
						(
							tpt.IsCash = 1
							AND COALESCE(i.BillingAmount, 0) + COALESCE(i.BillingFee, 0) > 0 
							AND pn.Id = @partnerid 
							AND i.CheckInPointId = @checkinpoint
							AND CAST(i.UserCreatedDate as date) = CAST(@date as date)
						)
				END
			END
            ");
        }
    }
}
