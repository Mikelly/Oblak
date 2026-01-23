using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Oblak.Data;
using Oblak.Models.Bankart;
using Oblak.Services;
using Oblak.Services.Bankart;

namespace Oblak.Controllers;

[Authorize]
[ApiController]
[Route("bankart")]
public sealed class BankartController(IHttpContextAccessor httpContextAccessor,
    ApplicationDbContext dbContext,
    IBankartService bankartService,
    ILogger<BankartController> logger,
    IConfiguration configuration,
    eMailService emailService) : Controller
{
    [HttpPost("initiatePayment")]
    public async Task<ActionResult<BankartInitiatePaymentOutput>> InitiatePayment(BankartInitiatePaymentInput input)
    {
        var legalEntity = await GetLegalEntityAsync();

        if (legalEntity is null)
        {
            Response.StatusCode = 401;
            return Json(new { info = "", error = "Korisnik nije ulogovan!" });
        }

        var transactionId = Guid.NewGuid().ToString();

        var (firstName, lastName) = ParseFullName(legalEntity.Name);

        var paymentResponse = await bankartService.InitiatePaymentAsync(new BankartServiceRequest
        {
            MerchantTransactionId = transactionId,
            Amount = 0.10m, // to-do, from group?
            SurchargeAmount = 0.10m, // to-do, from group?
            TransactionToken = input.Token,
            SuccessUrl = input.SuccessUrl,
            CancelUrl = input.CancelUrl,
            ErrorUrl = input.ErrorUrl,
            WithRegister = input.StoreCard,
            ReferenceUuid = input.ReferenceUuid,
            TestMode = legalEntity.Test,
            FirstName = firstName,
            LastName = lastName,
            BillingAddress1 = legalEntity.Address,
            Identification = legalEntity.TIN,
            Email = await GetAppUserEmailAsync()
        });

        var transaction = await dbContext.PaymentTransactions.AddAsync(new PaymentTransaction
        {
            Status = paymentResponse.ReturnType,
            Success = paymentResponse.Success,
            MerchantTransactionId = transactionId,
            GroupId = 1, // to-do,
            LegalEntityId = legalEntity.Id, // to-do, from group?
            PropertyId = 1, // to-do, from group?
            Token = input.Token,
            Type = BankartTransactionType.DEBIT.ToString(),
            Amount = 0.01m, // to-do, from group?
            SurchargeAmount = 0.01m, // to-do, from group?
            UserCreated = legalEntity.Name,
            UserCreatedDate = DateTime.UtcNow,
            WithRegister = input.StoreCard,
            ReferenceUuid = paymentResponse.Uuid
        });

        await dbContext.SaveChangesAsync();

        if (paymentResponse.Success)
        {
            return Json(new BankartInitiatePaymentOutput
            {
                RedirectUrl = paymentResponse.RedirectUrl,
                RedirectType = paymentResponse.RedirectType
            });
        }
        else
        {
            var errors = paymentResponse.Errors?.Select(x => x.AdapterMessage ?? x.ErrorMessage);

            return Json(new { info = "", error = errors });
        }
    }

    [AllowAnonymous]
    [HttpPost("storePaymentResult")]
    public async Task<ActionResult> StorePaymentResult([FromQuery] bool testMode = false)
    {
        try
        {
            logger.LogError("=== PAYTEN StorePaymentResult START === \n");
            logger.LogError("=== PAYTEN StorePaymentResult START === testMode: {TestMode}", testMode);

            // Read the request body
            var requestBody = await new StreamReader(Request.Body).ReadToEndAsync();

            if (string.IsNullOrEmpty(requestBody))
            {
                logger.LogError("=== PAYTEN StorePaymentResult - Request body is empty or null");
                return BadRequest("Request body is empty.");
            }

            JObject requestBodyObject;

            try
            {
                requestBodyObject = JObject.Parse(requestBody);
                logger.LogError("=== PAYTEN StorePaymentResult - Request body parsed successfully. Body: {RequestBody}", requestBody);
            }
            catch (Exception parseEx)
            {
                logger.LogError(parseEx, "=== PAYTEN StorePaymentResult - Failed to parse request body. Body: {RequestBody}", requestBody);
                return BadRequest("Invalid JSON format in request body.");
            }

            var apiKey = bankartService.GetConfigurationValue(testMode, "ApiKey");
            logger.LogError("=== PAYTEN StorePaymentResult - ApiKey retrieved: {ApiKeyExists}", !string.IsNullOrEmpty(apiKey));

            var requestUri = $"{Request.Path}{Request.QueryString}";
            logger.LogError("=== PAYTEN StorePaymentResult - Request URI: {RequestUri}", requestUri);

            var dateHeader = Request.Headers["Date"].FirstOrDefault();
            var xSignatureHeader = Request.Headers["X-Signature"].FirstOrDefault();

            logger.LogError("=== PAYTEN StorePaymentResult - Headers - Date: {DateHeader}, X-Signature: {XSignatureExists}",
                dateHeader ?? "NULL", !string.IsNullOrEmpty(xSignatureHeader));

            if (string.IsNullOrEmpty(dateHeader) || string.IsNullOrEmpty(xSignatureHeader))
            {
                logger.LogError("=== PAYTEN StorePaymentResult Missing required headers. === \n");
                logger.LogError("=== PAYTEN StorePaymentResult - Missing required headers");
                return BadRequest("Missing required headers.");
            }

            bool isValidSignature = bankartService.ValidateWebhookSignature(requestBody, requestUri, dateHeader, xSignatureHeader, testMode);

            if (!isValidSignature)
            {
                logger.LogError("=== PAYTEN StorePaymentResult Invalid signature. === \n");
                logger.LogError("=== PAYTEN StorePaymentResult - Invalid signature");
                return Unauthorized("Invalid signature.");
            }

            // fetch transaction from database
            var transactionId = requestBodyObject.SelectToken("merchantTransactionId")?.ToString();

            logger.LogError("=== PAYTEN StorePaymentResult - MerchantTransactionId extracted: {TransactionId}", transactionId ?? "NULL");

            if (string.IsNullOrEmpty(transactionId))
            {
                logger.LogError("=== PAYTEN StorePaymentResult - MerchantTransactionId is missing from request body");
                return BadRequest("Missing merchantTransactionId in request body.");
            }

            var transaction = await dbContext.PaymentTransactions
                .Include(pt => pt.LegalEntity)
                .FirstOrDefaultAsync(pt => pt.MerchantTransactionId == transactionId);

            if (transaction is null)
            {
                Response.StatusCode = 400;
                logger.LogError("=== PAYTEN StorePaymentResult Transaction not found. === \n");
                logger.LogError("=== PAYTEN StorePaymentResult - Transaction not found for ID: {TransactionId}", transactionId);
                return NotFound("Transaction not found.");
            }

            logger.LogError("=== PAYTEN StorePaymentResult - Transaction found: Id={TransactionId}, GroupId={GroupId}, Type={Type}",
                transaction.Id, transaction.GroupId, transaction.Type);

            // update transaction with payment result

            var result = requestBodyObject.SelectToken("result")?.ToString();

            var success = result == BankartResponseType.OK.ToString();

            logger.LogError("=== PAYTEN StorePaymentResult - Payment result: {Result}, Success: {Success}", result ?? "NULL", success);

            var now = DateTime.UtcNow;

            transaction.Status = result;
            transaction.Success = success;
            transaction.ResponseJson = requestBody;
            transaction.UserModifiedDate = now;

            var transactionType = requestBodyObject.SelectToken("transactionType")?.ToString();
            var lastFourDigits = requestBodyObject.SelectToken("returnData.lastFourDigits")?.ToString();
            var cardType = requestBodyObject.SelectToken("returnData.type")?.ToString();

            logger.LogError("=== PAYTEN StorePaymentResult - TransactionType: {TransactionType}, CardType: {CardType}, LastFourDigits: {LastFourDigits}",
                transactionType ?? "NULL", cardType ?? "NULL", lastFourDigits ?? "NULL");

            // If status is OK and transaction type is PREAUTHORIZE, void the amount,
            // deregister the old payment method and update PaymentMethods table
            if (success &&
                (transactionType == BankartTransactionType.PREAUTHORIZE.ToString() ||
                transaction.WithRegister == true))
            {
                logger.LogError("=== PAYTEN StorePaymentResult - Processing PREAUTHORIZE or WithRegister transaction");

                if (transactionType == BankartTransactionType.PREAUTHORIZE.ToString())
                {
                    logger.LogError("=== PAYTEN StorePaymentResult - Voiding PREAUTHORIZE transaction with ReferenceUuid: {ReferenceUuid}",
                        transaction.ReferenceUuid ?? "NULL");

                    var input = new BankartVoidTransactionInput
                    {
                        ReferenceUuid = transaction.ReferenceUuid!
                    };

                    _ = await VoidTransactionInternal(input,
                        transaction.LegalEntityId!.Value,
                        transaction.LegalEntity.Name,
                        transaction.LegalEntity.Test);

                    logger.LogError("=== PAYTEN StorePaymentResult - Void transaction completed");
                }

                var oldPaymentMethod = await dbContext.PaymentMethods
                    .Include(pm => pm.PaymentTransaction)
                    .FirstOrDefaultAsync(pm => pm.LegalEntityId == transaction.LegalEntityId);

                if (oldPaymentMethod != null)
                {
                    logger.LogError("=== PAYTEN StorePaymentResult - Old payment method found, deregistering. ReferenceUuid: {ReferenceUuid}",
                        oldPaymentMethod.PaymentTransaction.ReferenceUuid ?? "NULL");

                    var input = new BankartDeregisterPaymentMethodInput
                    {
                        ReferenceUuid = oldPaymentMethod.PaymentTransaction.ReferenceUuid!
                    };

                    _ = await DeletePaymentMethodInternal(input,
                        transaction.LegalEntityId!.Value,
                        transaction.LegalEntity.Name, transaction.LegalEntity.Test);

                    logger.LogError("=== PAYTEN StorePaymentResult - Old payment method deregistered");
                }
                else
                {
                    logger.LogError("=== PAYTEN StorePaymentResult - No old payment method found for LegalEntityId: {LegalEntityId}",
                        transaction.LegalEntityId);
                }

                logger.LogError("=== PAYTEN StorePaymentResult - Creating new payment method");

                var paymentMethod = await dbContext.PaymentMethods.AddAsync(new PaymentMethod
                {
                    PaymentTransactionId = transaction.Id,
                    LegalEntityId = transaction.LegalEntityId,
                    Type = cardType!,
                    LastFourDigits = lastFourDigits!,
                    UserCreated = transaction.LegalEntity.Name,
                    UserCreatedDate = now,
                });

                logger.LogError("=== PAYTEN StorePaymentResult - New payment method created");
            }

            // save changes
            logger.LogError("=== PAYTEN StorePaymentResult - Saving changes to database");

            await dbContext.SaveChangesAsync();

            logger.LogError("=== PAYTEN StorePaymentResult - Changes saved successfully");

            try
            {
                // if transaction type is DEBIT, send payment confirmation email
                // additionally we're making sure to send only for successful transactions, but unsuccesful transactions are also supported
                if (success && transactionType == BankartTransactionType.DEBIT.ToString())
                {
                    logger.LogError("=== PAYTEN StorePaymentResult - Processing email notification for DEBIT transaction");

                    var email = await dbContext.Users
                        .Where(x => x.LegalEntityId == transaction.LegalEntityId)
                        .Select(x => x.Email)
                        .FirstOrDefaultAsync();

                    logger.LogError("=== PAYTEN StorePaymentResult - User email retrieved: {Email}", email ?? "NULL");

                    var amount = requestBodyObject.SelectToken("totalAmount")?.ToString() ?? "N/A";
                    var currency = requestBodyObject.SelectToken("currency")?.ToString() ?? "N/A";
                    var authCode = requestBodyObject.SelectToken("extraData.authCode")?.ToString() ?? string.Empty;
                    lastFourDigits = lastFourDigits ?? "N/A";
                    cardType = cardType ?? "N/A";

                    var template = configuration["SendGrid:Templates:PaymentConfirmation"]!;
                    var senderEmail = configuration["SendGrid:EmailAddress"];

                    logger.LogError("=== PAYTEN StorePaymentResult - Sending payment confirmation email. Template: {Template}, Sender: {Sender}, Recipient: {Recipient}",
                        template ?? "NULL", senderEmail ?? "NULL", email ?? "NULL");

                    await emailService.SendMail(senderEmail!, email!, template!, new
                    {
                        subject = $@"donotreply: Informacije o Vašoj transakciji",
                        status = success ? $"Uspješno plaćanje" : $"Neuspješno plaćanje",
                        orderNumber = $"{transaction.MerchantTransactionId}",
                        amount = $"{amount} {currency}",
                        cardType = $"{cardType}",
                        lastFourDigits = $"{lastFourDigits}",
                        authCode = $"{authCode}",
                        timestamp = $"{transaction.UserCreatedDate.AddHours(2):yyyy-MM-dd HH:mm:ss}", // Format the timestamp,
                        success
                    });

                    logger.LogError("=== PAYTEN StorePaymentResult - Payment confirmation email sent successfully");
                }
                else
                {
                    logger.LogError("=== PAYTEN StorePaymentResult - Email notification skipped. Success: {Success}, TransactionType: {TransactionType}",
                        success, transactionType ?? "NULL");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send paymnent confirmation email.");
                logger.LogError(ex, "=== PAYTEN StorePaymentResult - Failed to send payment confirmation email");
            }

            logger.LogError("=== PAYTEN StorePaymentResult - Completed successfully, returning OK");

            return Content("OK");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment result.");

            logger.LogError(ex, "=== PAYTEN StorePaymentResult - Error processing payment result. Message: {Message}, StackTrace: {StackTrace}",
                ex.Message, ex.StackTrace);

            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpPost("storePaymentMethod")]
    public async Task<ActionResult<BankartInitiatePaymentOutput>> StorePaymentMethod(BankartRegisterPaymentMethodInput input)
    {
        var legalEntity = await GetLegalEntityAsync();

        if (legalEntity is null)
        {
            Response.StatusCode = 401;
            return Json(new { info = "", error = "Korisnik nije ulogovan!" });
        }

        var (firstName, lastName) = ParseFullName(legalEntity.Name);

        var transactionId = Guid.NewGuid().ToString();

        var paymentResponse = await bankartService.PreauthorizeTransactionAsync(new BankartServiceRequest
        {
            MerchantTransactionId = transactionId,
            TransactionToken = input.Token,
            SuccessUrl = input.SuccessUrl,
            CancelUrl = input.CancelUrl,
            ErrorUrl = input.ErrorUrl,
            TestMode = legalEntity.Test,
            FirstName = firstName,
            LastName = lastName,
            BillingAddress1 = legalEntity.Address,
            Identification = legalEntity.TIN,
            Amount = 0.01m,
            Email = await GetAppUserEmailAsync()
        });

        var transaction = await dbContext.PaymentTransactions.AddAsync(new PaymentTransaction
        {
            Status = paymentResponse.ReturnType,
            Success = paymentResponse.Success,
            MerchantTransactionId = transactionId,
            LegalEntityId = legalEntity.Id,
            Token = input.Token,
            Type = BankartTransactionType.PREAUTHORIZE.ToString(),
            UserCreated = legalEntity.Name,
            UserCreatedDate = DateTime.UtcNow,
            ReferenceUuid = paymentResponse.Uuid,
            Amount = 0.01m
        });

        await dbContext.SaveChangesAsync();

        if (paymentResponse.Success)
        {
            return Json(new BankartInitiatePaymentOutput
            {
                RedirectUrl = paymentResponse.RedirectUrl,
                RedirectType = paymentResponse.RedirectType
            });
        }
        else
        {
            var errors = paymentResponse.Errors.Select(e => e.AdapterMessage ?? e.ErrorMessage);

            return Json(new { info = "", error = errors });
        }
    }

    [HttpPost("getPaymentMethod")]
    public async Task<ActionResult> GetPaymentMethod()
    {
        var legalEntity = await GetLegalEntityAsync();

        if (legalEntity is null)
        {
            Response.StatusCode = 401;
            return Json(new { info = "", error = "Korisnik nije ulogovan!" });
        }

        var paymentMethod = await dbContext.PaymentMethods
            .Include(pm => pm.PaymentTransaction)
            .OrderByDescending(pm => pm.UserCreatedDate)
            .FirstOrDefaultAsync(pm => pm.LegalEntityId == legalEntity.Id);

        if (paymentMethod is null)
        {
            return Json(null);
        }

        var result = new BankartPaymentMethodDto
        {
            LastFourDigits = paymentMethod.LastFourDigits,
            ReferenceUuid = paymentMethod.PaymentTransaction.ReferenceUuid!,
            Type = paymentMethod.Type
        };

        return Json(result);
    }

    [HttpPost("deletePaymentMethod")]
    public async Task<ActionResult> DeletePaymentMethod(BankartDeregisterPaymentMethodInput input)
    {
        var legalEntity = await GetLegalEntityAsync();

        if (legalEntity is null)
        {
            Response.StatusCode = 401;
            return Json(new { info = "", error = "Korisnik nije ulogovan!" });
        }

        return await DeletePaymentMethodInternal(input, legalEntity.Id, legalEntity.Name, legalEntity.Test);
    }

    private async Task<ActionResult> VoidTransactionInternal(BankartVoidTransactionInput input, int legalEntityId,
        string legalEntityName,
        bool testMode)
    {
        var transactionId = Guid.NewGuid().ToString();

        var paymentResponse = await bankartService.VoidTransactionAsync(new BankartServiceRequest
        {
            MerchantTransactionId = transactionId,
            ReferenceUuid = input.ReferenceUuid,
            TestMode = testMode
        });

        var transaction = await dbContext.PaymentTransactions.AddAsync(new PaymentTransaction
        {
            Status = paymentResponse.ReturnType,
            Success = paymentResponse.Success,
            MerchantTransactionId = transactionId,
            LegalEntityId = legalEntityId,
            Type = BankartTransactionType.VOID.ToString(),
            UserCreated = legalEntityName,
            UserCreatedDate = DateTime.UtcNow,
            ReferenceUuid = input.ReferenceUuid
        });

        await dbContext.SaveChangesAsync();

        if (paymentResponse.Success)
        {
            return Ok();
        }
        else
        {
            var errors = paymentResponse.Errors.Select(e => e.AdapterMessage ?? e.ErrorMessage);

            return Json(new { info = "", error = errors });
        }
    }

    private async Task<ActionResult> DeletePaymentMethodInternal(BankartDeregisterPaymentMethodInput input,
        int legalEntityId,
        string legalEntityName,
        bool testMode)
    {
        var transactionId = Guid.NewGuid().ToString();

        var paymentResponse = await bankartService.DeregisterPaymentMethodAsync(new BankartServiceRequest
        {
            MerchantTransactionId = transactionId,
            ReferenceUuid = input.ReferenceUuid,
            TestMode = testMode
        });

        _ = await dbContext.PaymentMethods
            .Where(pm => pm.LegalEntityId == legalEntityId)
            .ExecuteDeleteAsync();

        var transaction = await dbContext.PaymentTransactions.AddAsync(new PaymentTransaction
        {
            Status = paymentResponse.ReturnType,
            Success = paymentResponse.Success,
            MerchantTransactionId = transactionId,
            LegalEntityId = legalEntityId,
            Type = BankartTransactionType.DEREGISTER.ToString(),
            UserCreated = legalEntityName,
            UserCreatedDate = DateTime.UtcNow,
            ReferenceUuid = input.ReferenceUuid
        });

        await dbContext.SaveChangesAsync();

        if (paymentResponse.Success)
        {
            return Ok();
        }
        else
        {
            var errors = paymentResponse.Errors.Select(e => e.AdapterMessage ?? e.ErrorMessage);

            return Json(new { info = "", error = errors });
        }
    }

    private async Task<LegalEntity?> GetLegalEntityAsync()
    {
        var username = httpContextAccessor.HttpContext?.User.Identity?.Name;

        if (string.IsNullOrEmpty(username))
        {
            return null;
        }

        var appUser = await dbContext.Users
            .Include(u => u.LegalEntity)
                .ThenInclude(le => le.Properties)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (appUser is null)
        {
            return null;
        }

        return appUser.LegalEntity;
    }

    private async Task<string> GetAppUserEmailAsync()
    {
        var username = httpContextAccessor.HttpContext?.User.Identity?.Name
            ?? throw new InvalidOperationException("Korisničko ime nije pronadjeno!");

        return await dbContext.Users
            .Where(u => u.UserName == username)
            .Select(u => u.Email)
            .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("Email nije pronadjen!");
    }

    private static (string, string) ParseFullName(string name)
    {
        var names = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var firstName = names.Length > 1 ? string.Join(' ', names.Take(names.Length - 1)) : names.FirstOrDefault() ?? string.Empty;

        var lastName = names.Length > 1 ? names.Last() : string.Empty;

        return (firstName, lastName);
    }
}
