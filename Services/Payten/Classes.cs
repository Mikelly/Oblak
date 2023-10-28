namespace Oblak.Services.Payten
{
    public class Error
    {
        public object errorCode { get; set; }
        public string description { get; set; }
    }


    public class UserHashmapProviderRequest
    {
        public string UserHash { get; set; }
        public string ProviderName { get; set; }
        public string ProviderKey { get; set; }
        public string ProviderValue { get; set; }
    }


    #region Authorize

    public class AuthorizeRequest
    {
        public string ApplicationLoginID { get; set; }
        public string Password { get; set; }
    }

    public class AuthorizeResponse
    {
        public string Token { get; set; }
    }

    #endregion


    #region CreatePaymentSessionTokenRequest

    public class CreatePaymentSessionTokenRequest
    {
        public string UserHash { get; set; }
        public object Amount { get; set; }
        public string OrderID { get; set; }
        public string CurrencyCode { get; set; }
        public string TransactionType { get; set; }
        public string CallBackURL { get; set; }
        public Additionalamount[] AdditionalAmounts { get; set; }
    }

    public class Additionalamount
    {
        public string Caption { get; set; }
        public string IntegrationKey { get; set; }
        public object Amount { get; set; }
    }

    public class CreatePaymentSessionTokenResponse
    {
        public string PaymentSessionToken { get; set; }
    }

    #endregion


    #region GetPaymentSessionStatus

    public class GetPaymentSessionStatusRequest
    {
        public string PaymentSessionToken { get; set; }
    }


    public class GetPaymentSessionStatusResponse
    {
        public string StatusCode { get; set; }
        public StatusHistory[] StatusHistory { get; set; }
    }

    public class StatusHistory
    {
        public string StatusCode { get; set; }
        public object CreateDate { get; set; }
    }

    #endregion


    #region GetTransactionByPaymentSessionToken

    public class GetTransactionByPaymentSessionTokenRequest
    {
        public string PaymentSessionToken { get; set; }
    }

    public class GetTransactionByPaymentSessionTokenResponse
    {
        public string ScreenMessage { get; set; }
        public string Rrn { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionDateISO { get; set; }
        public Receipt[] Receipt { get; set; }
        public object Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string TransactionCode { get; set; }
        public object RecordId { get; set; }
        public string MaskedPan { get; set; }
        public string AuthorizationCode { get; set; }
        public string ResponseCodeValue { get; set; }
        public string StatusCode { get; set; }
        public string HexColor { get; set; }
        public string ResultText { get; set; }
        public bool IsVoidable { get; set; }
        public string IsRefundable { get; set; }
        public string BankResponseCodeValue { get; set; }
        public string IssuerBin { get; set; }
        public string AcquirerId { get; set; }
        public object ClearingDate { get; set; }
    }

    public class Receipt
    {
        public Detail[] Detail { get; set; }
        public string PaymentBrandUrl { get; set; }
        public string MerchantLogoURL { get; set; }
        public string SchemaName { get; set; }
        public bool Approved { get; set; }
    }

    public class Detail
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string ColorHex { get; set; }
        public bool ShowCheck { get; set; }
        public bool ShowTimes { get; set; }
    }

    #endregion
}