using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class CreateTouristOrgNauticalTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
			CREATE PROCEDURE [dbo].[TouristOrgNauticalTax]
				@partnerid int,	
				@dateFrom datetime,	
				@dateTo datetime	
			AS
			BEGIN

				SET NOCOUNT ON;
				SELECT 		
					FORMAT(ROW_NUMBER() OVER (ORDER BY g.UserCreatedDate), '00000') OrderNo,
					cast(g.UserCreatedDate as date) CreatedDate,
					v.OwnerName, 
					v.name, 
					g.CheckIn,
					g.CheckOut,
					v.Length, 
					OwnerAddress, 
					g.ResTaxAmount AS Tax,					
					g.ResTaxFee AS Fee
					FROM Groups g 
					INNER JOIN Properties p ON g.PropertyId = p.Id 
					INNER JOIN LegalEntities le ON le.Id = p.LegalEntityId
					INNER JOIN Partners pn ON pn.Id = le.PartnerId			
					LEFT JOIN ResTaxPaymentTypes rtp ON rtp.Id = g.ResTaxPaymentTypeId	
					LEFT JOIN CheckInPoints cp ON cp.Id = g.CheckInPointId
					LEFT JOIN PartnerTaxSettings s ON s.PartnerId = pn.Id AND s.TaxType = 'NauticalTax'
					LEFT JOIN Vessels v ON v.Id = g.VesselId
					LEFT JOIN Countries c ON c.Id = v.CountryId
					WHERE 
					(		
						g.VesselId IS NOT NULL
						AND rtp.PaymentStatus = 'Cash' 
						AND COALESCE(g.ResTaxAmount, 0) + COALESCE(g.ResTaxFee, 0) > 0 
						AND le.PartnerId = @partnerid
						AND CAST(g.UserCreatedDate as date) between CAST(@dateFrom as date) and CAST(@dateTo as date)
					) order by g.UserCreatedDate

			END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP PROCEDURE [dbo].[TouristOrgNauticalTax]");
        }
    }
}
