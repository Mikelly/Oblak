using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oblak.Migrations
{
    /// <inheritdoc />
    public partial class AddCalcLegalEntityDebtFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                    CREATE FUNCTION dbo.CalcLegalEntityDebt
                    (
                    @legalEntityId INT,
                    @partner INT = 4 
                    )
                    RETURNS DECIMAL(18,2)
                    AS
                    BEGIN
                    DECLARE @Saldo DECIMAL(18,2) = 0;
	 
                    WITH SaldoData AS 
                    (
                    -- MnePersons
                    SELECT 
                        m.LegalEntityId AS Id,
                        m.ResTaxAmount AS Duguje,
                        CASE 
                            WHEN rp.PaymentStatus = 'Cash' THEN m.ResTaxAmount 
                            WHEN rp.PaymentStatus = 'AlreadyPaid' THEN pay.Amount
                            WHEN rp.PaymentStatus = 'Unpaid' THEN 0
                        END AS Potrazuje
                    FROM MnePersons m
                    INNER JOIN Properties p ON p.Id = m.PropertyId
                    INNER JOIN LegalEntities le ON le.Id = p.LegalEntityId
                    INNER JOIN ResTaxPaymentTypes rp ON rp.Id = m.ResTaxPaymentTypeId
                    LEFT JOIN TaxPayments pay ON pay.PersonId = m.Id
                    WHERE le.PartnerId = @partner
                        AND le.Id = @legalEntityId
                        AND m.GroupId IS NULL
                        AND m.ResTaxAmount > 0
                        AND CAST(m.UserCreatedDate AS DATE) < GETDATE() - 30

                    UNION ALL

                    -- Groups
                    SELECT 
                        g.LegalEntityId AS Id,
                        g.ResTaxAmount AS Duguje,
                        CASE 
                            WHEN rp.PaymentStatus = 'Cash' THEN g.ResTaxAmount 
                            WHEN rp.PaymentStatus = 'AlreadyPaid' THEN pay.Amount
                            WHEN rp.PaymentStatus = 'Unpaid' THEN 0
                        END AS Potrazuje
                    FROM Groups g
                    INNER JOIN Properties p ON p.Id = g.PropertyId
                    INNER JOIN LegalEntities le ON le.Id = p.LegalEntityId
                    INNER JOIN ResTaxPaymentTypes rp ON rp.Id = g.ResTaxPaymentTypeId
                    LEFT JOIN TaxPayments pay ON pay.GroupId = g.Id
                    WHERE le.PartnerId = @partner
                        AND le.Id = @legalEntityId
                        AND g.ResTaxAmount > 0
                        AND CAST(g.UserCreatedDate AS DATE) < GETDATE() - 30

                    UNION ALL

                    -- TaxPayments
                    SELECT 
                        pay.LegalEntityId AS Id,
                        0 AS Duguje,
                        pay.Amount AS Potrazuje
                    FROM TaxPayments pay
                    INNER JOIN LegalEntities le ON le.Id = pay.LegalEntityId
                    INNER JOIN TaxPaymentTypes t ON t.Id = pay.TaxPaymentTypeId
                    WHERE pay.PersonId IS NULL
                        AND le.Id = @legalEntityId
                        AND pay.GroupId IS NULL
                        AND CAST(pay.TransactionDate AS DATE) < GETDATE() - 30
                    )
	 
                    SELECT @Saldo = SUM(Duguje - Potrazuje) FROM SaldoData;

                    RETURN @Saldo;
                    END; 
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS dbo.CalcLegalEntityDebt;");
        }
    }
}
