using Microsoft.EntityFrameworkCore;
using Oblak.Data;
using Oblak.Data.Enums;
using Oblak.Models.Api;
using System.Linq;

namespace Oblak.Services;

public class GroupService
{
    readonly ApplicationDbContext _db;

    public GroupService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> CalculateResTaxFee(Group group)
    {
        // Return the fee directly if it is already set
        if (group.ResTaxFee.HasValue)
        {
            return group.ResTaxFee.Value;
        }

        // If there's no fee set but there's a tax amount and payment type, calculate the fee
        if (group.ResTaxAmount.HasValue && group.ResTaxPaymentTypeId.HasValue)
        {
            var amount = group.ResTaxAmount.Value;
            var fee = await _db.ResTaxFees
                .Where(f => f.ResTaxPaymentTypeId == group.ResTaxPaymentTypeId && f.LowerLimit <= amount && f.UpperLimit >= amount)
                .FirstOrDefaultAsync();

            if (fee != null)
            {
                return fee.FeeAmount ?? (amount * (fee.FeePercentage ?? 0) / 100);
            }
        }

        // Default return value if no fee is applicable
        return 0;
    }

    public async Task<Dictionary<int, string>> GetPaymentStatusForGroupsAsync(List<int> groupIds)
    {
        var latestTransactions = await _db.PaymentTransactions
            .Where(pt => pt.GroupId.HasValue &&
                groupIds.Contains(pt.GroupId.Value))
            .GroupBy(pt => pt.GroupId)
            .Select(g => g.OrderByDescending(pt => pt.UserCreatedDate).FirstOrDefault())
            .ToListAsync();

        var statuses = new Dictionary<int, string>();

        // Loop over the existing transactions
        foreach (var transaction in latestTransactions)
        {
            statuses[transaction.GroupId.Value] = transaction.Status switch
            {
                nameof(PaymentResponseTypes.OK) => PaymentStatusTypes.Finished.ToString(),
                nameof(PaymentResponseTypes.REDIRECT) => PaymentStatusTypes.InProgress.ToString(),
                nameof(PaymentResponseTypes.ERROR) => PaymentStatusTypes.Error.ToString(),
                _ => PaymentStatusTypes.Pending.ToString()
            };
        }

        // Set pending status for groups with no transactions
        foreach (var groupId in groupIds)
        {
            if (!statuses.ContainsKey(groupId))
            {
                statuses[groupId] = PaymentStatusTypes.Pending.ToString();
            }
        }

        return statuses;
    }

    public async Task<GroupPaymentInfoDto> GetPaymentInfoForGroupAsync(int groupId)
    {
        var latestTransaction = await _db.PaymentTransactions
            .Where(pt => pt.GroupId == groupId && pt.Type == PaymentTransactionTypes.DEBIT.ToString())
            .OrderByDescending(pt => pt.UserCreatedDate)
            .FirstOrDefaultAsync();

        if (latestTransaction == null)
        {
            return new GroupPaymentInfoDto
            {
                PaymentStatus = PaymentStatusTypes.Pending.ToString()
            };
        }

        var paymentStatus = latestTransaction.Status switch
        {
            nameof(PaymentResponseTypes.OK) => PaymentStatusTypes.Finished.ToString(),
            nameof(PaymentResponseTypes.REDIRECT) => PaymentStatusTypes.InProgress.ToString(),
            nameof(PaymentResponseTypes.ERROR) => PaymentStatusTypes.Error.ToString(),
            _ => PaymentStatusTypes.Pending.ToString()
        };

        return new GroupPaymentInfoDto
        {
            MerchantTransactionId = latestTransaction.MerchantTransactionId,
            PaymentStatus = paymentStatus,
            Timestamp = latestTransaction.UserCreatedDate,
            PaymentResponse = latestTransaction.ResponseJson
        };
    }
}
