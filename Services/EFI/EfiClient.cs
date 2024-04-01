using EfiService;
using Oblak.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Xml;
using System.Text;
using Oblak.Models.EFI;
using Kendo.Mvc.Extensions;
using Oblak.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Telerik.Windows.Documents.Flow.FormatProviders.Docx;
using Telerik.Windows.Documents.Flow.Model.Editing;
using Telerik.Windows.Documents.Flow.Model;
using Telerik.Windows.Documents.Flow.FormatProviders.Pdf;
using Oblak.Services;
using System.Xml.Serialization;
using Oblak.Data.Enums;
using System.Security.Policy;
using Net.Codecrete.QrCodeGenerator;

namespace Oblak.Services;

public class EfiClient
{
    const string XML_SCHEMA_NS = "https://efi.tax.gov.me/fs/schema";
    const string XML_REQUEST_ID = "Request";
    const string XML_SIG_METHOD = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    const string XML_DIG_METHOD = "http://www.w3.org/2001/04/xmlenc#sha256";

    readonly IWebHostEnvironment _env;
    readonly HttpContext _context;
    readonly ILogger<EfiClient> _logger;
    readonly ApplicationDbContext _db;
    readonly IConfiguration _config;
    readonly ApplicationUser _appUser;
    readonly eMailService _eMailService;
    readonly DocumentService _documentService;
    readonly IWebHostEnvironment _webHostEnvironment;

    public EfiClient(
        IConfiguration config,
        IHttpContextAccessor contextAccesor,
        IWebHostEnvironment env,        
        ILogger<EfiClient> logger,
        ApplicationDbContext db,
        eMailService eMailService,
        DocumentService documentService,
        IWebHostEnvironment webHostEnvironment
        ) 
    {
        _config = config;
        _context = contextAccesor.HttpContext!;
        _env = env;
        _logger = logger;
        _db = db;
        _eMailService = eMailService;
        _webHostEnvironment = webHostEnvironment;
        _documentService = documentService;
        _appUser = _db.Users.Include(a => a.LegalEntity).FirstOrDefault(a => a.UserName == _context.User.Identity!.Name)!;
    }

    public async Task<RegisterInvoiceRequest> Fiscalize(Document doc, string correct, string late)
    {  
        if (doc.Status == DocumentStatus.Fiscalized) throw new Exception("Račun je već fiskalizovan!");

        var enu = _db.FiscalEnu.FirstOrDefault(a => a.FiscalEnuCode == doc.FiscalEnuCode)!;

        if(doc.InvoiceDate.Date != DateTime.Now.Date) doc.InvoiceDate = DateTime.Now.Date;

        if (doc.TypeOfInvoce == TypeOfInvoice.Cash && enu.AutoDeposit.HasValue)
        {
            var deposit = _db.FiscalRequests.Any(a => a.FiscalEnuCode == doc.FiscalEnuCode && a.FicalizationDate.Date == doc.InvoiceDate.Date && a.RequestType == Data.Enums.FiscalRequestType.RegisterCashDeposit && a.FCDC != null);
            if (deposit == false)
            {
                var amount = enu.AutoDeposit.Value;
                await Cash(amount, "i", doc.FiscalEnuCode, doc.BusinessUnitCode);
            }
        }

        var url = GetFiscalParameter("URL", _appUser!.LegalEntity.Test);

        (doc.No, doc.OrdinalNo) = _documentService.DocumentNumbers(doc.BusinessUnitCode);
        if ((late ?? "") == "") doc.InvoiceDate = DateTime.Now;
        doc.ExternalNo = _documentService.ExternalNumber(doc);
        _db.SaveChanges();
        

        // ------- GENERISANJE REQUESTA --------
        _logger.LogDebug("CREATE REQUEST");
        var request = Invoice(doc, correct, late);     

        System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => { return true; };

        System.ServiceModel.Channels.Binding binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport);
        System.ServiceModel.EndpointAddress endpoint = new System.ServiceModel.EndpointAddress(url);
        FiscalizationServicePortTypeClient client = new FiscalizationServicePortTypeClient(binding, endpoint);
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        FiscalRequest fr = null;
        RegisterInvoiceResponse response = null;

        _logger.LogDebug("SIGN REQUEST");

        var xml = SignInvoiceRequest(request);

        _logger.LogDebug("SIGNED");

        try
        {
            fr = new FiscalRequest();
            _db.FiscalRequests.Add(fr);
            fr.RequestType = FiscalRequestType.RegisterInvoice;
            fr.Invoice = doc.Id;
            fr.InvoiceNo = doc.No;
            fr.LegalEntityId = doc.LegalEntityId;
            fr.FiscalEnuCode = doc.FiscalEnuCode;            
            fr.BusinessUnitCode = doc.BusinessUnitCode;
            fr.Amount = request.Invoice.TotPrice;            
            fr.FicalizationDate = request.Header.SendDateTime;
            fr.Status = "A";
            doc.Status = DocumentStatus.Active;            
            doc.IIC = request.Invoice.IIC;
            doc.Amount = request.Invoice.TotPrice;
            doc.FiscalNo = request.Invoice.InvNum;
            doc.FiscalizationDate = request.Header.SendDateTime;
            fr.Request = xml;            
            fr.IIC = request.Invoice.IIC;

            var inv = request.Invoice;
            var d = inv.IssueDateTime;
            var dtm = $"{d.ToString("yyyy")}-{d.ToString("MM")}-{d.ToString("dd")}T{d.ToString("HH")}:{d.ToString("mm")}:{d.ToString("ss")}{d.ToString("zzz")}";
            var qrurl = GetFiscalParameter("QR", _appUser!.LegalEntity.Test);
            doc.Qr = $@"{qrurl}/ic/#/verify?iic={inv.IIC}&tin={inv.Seller.IDNum}&crtd={dtm}&ord={inv.InvOrdNum}&bu={inv.BusinUnitCode}&cr={inv.TCRCode}&sw={inv.SoftCode}&prc={inv.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace(",", "")}";
            _db.SaveChanges();

            //var qr = QrCode.EncodeText(doc.Qr, QrCode.Ecc.Medium);
            //var path = Path.Combine(_env.WebRootPath, $"QR/{doc.Id}.png");
            //qr.SaveAsPng(path, 10, 3);
            //_db.SaveChanges();
            
            response = client.registerInvoice(request);
                        
            fr.Response = response.ToXML();
            fr.FIC = response.FIC.ToUpper().Replace("-", "");
            fr.Status = "F";
            doc.FIC = fr.FIC;
            doc.Status = DocumentStatus.Fiscalized;              
            _db.SaveChanges();
        }
        catch (Exception excp)
        {
            _logger.LogDebug("fiscal error: " + excp.Message);

            string error = "";

            if (excp is System.ServiceModel.FaultException)
            {
                error = Exceptions.ParseException(excp);
                doc.Status = DocumentStatus.Active;
                throw new Exception(error);
            }
            else
            {
                error = Exceptions.StringException(excp);
                doc.Status = DocumentStatus.NotFiscalized;
                fr.Error = error;
            }
        }
        finally
        {
            _db.SaveChanges();
        }

        return request;
    }


    public async Task<RegisterCashDepositRequest> Cash(decimal amount, string type, string enu, string bu)
    {
        var url = GetFiscalParameter("URL", _appUser!.LegalEntity.Test);

        System.Net.ServicePointManager.ServerCertificateValidationCallback += (se, cert, chain, sslerror) => { return true; };

        System.ServiceModel.Channels.Binding binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport);
        System.ServiceModel.EndpointAddress endpoint = new System.ServiceModel.EndpointAddress(url);
        FiscalizationServicePortTypeClient client = new FiscalizationServicePortTypeClient(binding, endpoint);
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        var cash = new RegisterCashDepositRequest();
        cash.Id = "Request";
        cash.Version = 1;
        cash.Header = new RegisterCashDepositRequestHeaderType();
        cash.Header.SendDateTime = DateTime.Now.forXML(); ;
        cash.Header.UUID = Guid.NewGuid().ToString();
        cash.CashDeposit = new CashDepositType();
        cash.CashDeposit.CashAmt = amount.Round2();
        cash.CashDeposit.Operation = type.ToLower() == "i" ? CashDepositOperationSType.INITIAL : CashDepositOperationSType.WITHDRAW;
        cash.CashDeposit.TCRCode = enu;
        cash.CashDeposit.IssuerTIN = _appUser!.LegalEntity.TIN;
        cash.CashDeposit.ChangeDateTime = cash.Header.SendDateTime;

        var xml = SignCashRequest(cash);

        RegisterCashDepositResponse result = null;
        FiscalRequest fr = null;

        try
        {
            fr = new FiscalRequest();
            _db.FiscalRequests.Add(fr);
            fr.RequestType = type.ToLower() == "i" ? FiscalRequestType.RegisterCashDeposit : FiscalRequestType.RegisterCashWithdraw;
            fr.LegalEntityId = _appUser.LegalEntityId;            
            fr.FiscalEnuCode = enu;            
            fr.BusinessUnitCode = bu;
            fr.Amount = amount;
            fr.FicalizationDate = DateTime.Now;
            fr.Request = xml;            

            result = client.registerCashDeposit(cash);

            fr.FCDC = result.FCDC;            
            fr.Response = result.ToXML();
            fr.Status = "F";

            return cash;
        }
        catch (Exception excp)
        {
            string error = "";
            if (excp is System.ServiceModel.FaultException)
            {
                error = Exceptions.ParseException(excp);
            }
            else error = Exceptions.StringException(excp);
            fr.Error = error;
            throw new Exception(error);
        }
        finally
        {
            _db.SaveChanges();
        }
    }


    public RegisterInvoiceRequest Invoice(Document doc, string correct = null, string late = null)
    {            
        
        var tcr = _context.Session.GetString("ENU")! ?? "";
        var now = DateTime.Now;

        var oj = doc.Property;

        var inv = new RegisterInvoiceRequest();
        inv.Header = new RegisterInvoiceRequestHeaderType();
        inv.Id = "Request";
        inv.Version = 1;
        inv.Invoice = new EfiService.InvoiceType();

        // DEFINICIJA TIPA RACUNA...
        inv.Invoice.InvType = InvoiceTSType.INVOICE;
        if (correct != null) inv.Invoice.InvType = InvoiceTSType.CORRECTIVE;                                   

        inv.Header.UUID = Guid.NewGuid().ToString();
        inv.Header.SendDateTime = now.forXML();

        inv.Invoice.TypeOfInv = InvoiceSType.NONCASH;
        inv.Invoice.TypeOfInv = InvoiceSType.CASH;
        inv.Invoice.TCRCode = doc.FiscalEnuCode;
        if (late == null || late == "TAXPERIOD") inv.Invoice.IssueDateTime = now.forXML();
        inv.Invoice.BusinUnitCode = doc.BusinessUnitCode;
        inv.Invoice.InvOrdNum = doc.OrdinalNo;
        inv.Invoice.InvNum = $"{inv.Invoice.BusinUnitCode}/{inv.Invoice.InvOrdNum}/{inv.Invoice.IssueDateTime.Year}/{inv.Invoice.TCRCode}";
        inv.Invoice.IsIssuerInVAT = _appUser!.LegalEntity.InVat;

        if (late != null && late != "PARAGON" && late != "TAXPERIOD")
        {
            inv.Header.SubseqDelivType = late == "TECHICALERROR" ? SubseqDelivTypeSType.TECHNICALERROR : late == "NOINTERNET" ? SubseqDelivTypeSType.NOINTERNET : late == "BUSINESSNEEDS" ? SubseqDelivTypeSType.BUSINESSNEEDS : SubseqDelivTypeSType.TECHNICALERROR;
            inv.Header.SubseqDelivTypeSpecified = true;
            inv.Invoice.IssueDateTime = doc.InvoiceDate.forXML();
        }            

        if (late == "TAXPERIOD")
        {
            inv.Invoice.TaxPeriod = doc.InvoiceDate.ToString("MM") + "/" + doc.InvoiceDate.ToString("yyyy");
        }
        else
        {
            inv.Invoice.TaxPeriod = inv.Invoice.IssueDateTime.ToString("MM") + "/" + inv.Invoice.IssueDateTime.ToString("yyyy");
        }

        var fenu = _db.FiscalEnu.FirstOrDefault(a => a.FiscalEnuCode == doc.FiscalEnuCode);
        var oper = fenu.OperatorCode;
        if ((oper ?? "") == "")
        {
            throw new Exception("Neispravno definisan operator!");
        }

        inv.Invoice.OperatorCode = oper;            
        inv.Invoice.SoftCode = GetFiscalParameter("SFT", _appUser!.LegalEntity.Test);
        inv.Invoice.IsReverseCharge = false;

        inv.Invoice.Currency = new CurrencyType();
        inv.Invoice.Currency.Code = doc.CurrencyCode == "USD" ? CurrencyCodeSType.USD : doc.CurrencyCode == "GBP" ? CurrencyCodeSType.GBP : CurrencyCodeSType.EUR;
        inv.Invoice.Currency.ExRate = (double)(doc.ExchangeRate == 0 ? 1 : doc.ExchangeRate);

        inv.Invoice.Seller = new SellerType();
        inv.Invoice.Seller.IDType = IDTypeSType.TIN;
        inv.Invoice.Seller.IDNum = _appUser.LegalEntity.TIN;
        inv.Invoice.Seller.Name = _appUser.LegalEntity.Name;
        inv.Invoice.Seller.Address = _appUser.LegalEntity.Address;
        
        inv.Invoice.Buyer = new EfiService.BuyerType();
        if ((doc.PartnerIdNumber ?? "") != "" && (doc.PartnerIdType.ToString() ?? "") != "" && (doc.PartnerName ?? "") != "")
        {
            inv.Invoice.Buyer.IDType = doc.PartnerIdType switch
            { 
                BuyerIdType.ID => IDTypeSType.ID,
                BuyerIdType.Passport => IDTypeSType.PASS,
                _ => IDTypeSType.TIN,
            };
            inv.Invoice.Buyer.IDNum = doc.PartnerIdNumber;
            inv.Invoice.Buyer.Name = doc.PartnerName;
        }
        else
        {
            inv.Invoice.Buyer.IDType = IDTypeSType.TIN;                    
            inv.Invoice.Buyer.IDNum = "00000000";
            inv.Invoice.Buyer.Name = "Fizičko lice";
        }            

        List<DocumentItem> stavke = new List<DocumentItem>();
        List<DocumentPayment> placanja = new List<DocumentPayment>();            

        inv.Invoice.TaxFreeAmt = 0;
        inv.Invoice.MarkUpAmt = 0;
        inv.Invoice.GoodsExAmt = 0;

        stavke = doc.DocumentItems.ToList();
        placanja = doc.DocumentPayments.ToList();                

        inv.Invoice.TotPrice = stavke.Select(a => a.LineTotal).Sum().Round2();
        inv.Invoice.TotPriceWoVAT = stavke.Select(a => a.LineTotalWoVat).Sum().Round2();

        if (inv.Invoice.IsIssuerInVAT)
        {
            inv.Invoice.TotVATAmt = stavke.Select(a => a.LineTotal - a.LineTotalWoVat).Sum().Round2();
            inv.Invoice.TotVATAmtSpecified = true;            
        }
        else
        {
            inv.Invoice.TaxFreeAmt = inv.Invoice.TotPriceWoVAT;
            inv.Invoice.TaxFreeAmtSpecified = true;
        }

        if (correct != null)
        {
            var original = _db.Documents.SingleOrDefault(a => a.IIC == correct);
            inv.Invoice.CorrectiveInv = new CorrectiveInvType();
            inv.Invoice.CorrectiveInv.Type = CorrectiveInvTypeSType.CORRECTIVE;
            inv.Invoice.CorrectiveInv.IICRef = correct;
            inv.Invoice.CorrectiveInv.IssueDateTime = original.InvoiceDate.forXML();
        }

        #region Placanja

        var payments = new List<PayMethodType>();
        if (placanja.Any())
        {
            foreach (var p in placanja)
            {
                var pay = new PayMethodType();
                pay.Amt = p.Amount.Round2();                        
                pay.Type = p.PaymentType switch
                {
                    PaymentType.Cash => PaymentMethodTypeSType.BANKNOTE,
                    PaymentType.BankAccount => PaymentMethodTypeSType.ACCOUNT,
                    PaymentType.CreditCard => PaymentMethodTypeSType.CARD,
                    PaymentType.Advance => PaymentMethodTypeSType.ADVANCE,
                    _ => PaymentMethodTypeSType.OTHER,
                };                        
                payments.Add(pay);
            }
            if (payments.All(a => a.Type == PaymentMethodTypeSType.ACCOUNT || a.Type == PaymentMethodTypeSType.ADVANCE)) inv.Invoice.TypeOfInv = InvoiceSType.NONCASH;
            else if (payments.All(a => a.Type == PaymentMethodTypeSType.BANKNOTE || a.Type == PaymentMethodTypeSType.CARD)) inv.Invoice.TypeOfInv = InvoiceSType.CASH;
        }

        inv.Invoice.PayMethods = payments.GroupBy(a => new { a.Type, AdvIIC = a.Type == PaymentMethodTypeSType.ADVANCE ? payments.Where(b => b.AdvIIC != null).Select(b => b.AdvIIC).FirstOrDefault() : null }).Select(a => new PayMethodType() { Type = a.Key.Type, AdvIIC = a.Key.AdvIIC, Amt = a.Sum(b => b.Amt) }).ToArray();
            
        #endregion

        #region Stavke

        var items = new List<InvoiceItemType>();

        foreach (var s in stavke)
        {
            var itm = new InvoiceItemType();
            itm.C = s.ItemCode.Trim().GetFirst(50);
            itm.N = s.ItemName.Trim().GetFirst(50);
            itm.Q = (double)s.Quantity; // Kolicina
            itm.U = s.ItemUnit; // Jedinica Mjere
            itm.VR = (s.VatRate).Round4(); // Stopa PDV-a
            itm.UPA = (s.FinalPrice).Round4(); // Cijena sa PDV
            itm.UPB = (s.FinalPrice / ((1 + s.VatRate / 100m) * (1m - s.Discount / 100m))).Round4(); // Cijena bez PDV

            if (s.VatExempt != null)
            {
                itm.EX = s.VatExempt switch
                {
                    MneVatExempt.VAT_CL17 => ExemptFromVATSType.VAT_CL17,
                    MneVatExempt.VAT_CL20 => ExemptFromVATSType.VAT_CL20,
                    MneVatExempt.VAT_CL26 => ExemptFromVATSType.VAT_CL26,
                    MneVatExempt.VAT_CL27 => ExemptFromVATSType.VAT_CL27,
                    MneVatExempt.VAT_CL28 => ExemptFromVATSType.VAT_CL28,
                    MneVatExempt.VAT_CL29 => ExemptFromVATSType.VAT_CL29,
                    MneVatExempt.VAT_CL30 => ExemptFromVATSType.VAT_CL30,
                    _ => ExemptFromVATSType.VAT_CL20,
                };
                itm.EXSpecified = true;
            }

            itm.R = (s.Discount).Round2(); // Rabat u procentima
            itm.RR = false; // Da li rabat smanjuje cijenu
            itm.RSpecified = true;
            itm.RRSpecified = true;

            itm.PA = (s.LineTotal).Round4(); // Cijena sa PDV-a
            itm.VA = (s.LineTotal - s.LineTotalWoVat).Round4(); // Iznos PDV-a
            itm.PB = (s.LineTotalWoVat).Round4(); // Iznos bez PDV-a                    

            itm.VRSpecified = true;
            itm.VASpecified = true;
            items.Add(itm);

        }
        inv.Invoice.Items = items.ToArray();                

        #endregion

        #region Porezi

        var taxes = new List<SameTaxType>();

        var porezi = stavke.GroupBy(a => a.VatRate);

        if (inv.Invoice.IsIssuerInVAT)
        {
            foreach (var p in items.GroupBy(a => new { a.EX, a.EXSpecified, a.VR }))
            {
                var tax = new SameTaxType();
                tax.VATRate = p.Key.VR.Round2();                        
                tax.NumOfItems = p.Count();
                tax.PriceBefVAT = (p.Select(a => a.PB).Sum()).Round2();
                tax.VATAmt = (p.Select(a => a.VA).Sum()).Round2();                        
                if (p.Key.EXSpecified)
                {
                    tax.ExemptFromVAT = p.Key.EX;
                    tax.ExemptFromVATSpecified = true;
                    tax.VATRateSpecified = false;
                    tax.VATAmtSpecified = false;
                }
                else
                {
                    tax.ExemptFromVATSpecified = false;
                    tax.VATRateSpecified = true;
                    tax.VATAmtSpecified = true;
                }
                taxes.Add(tax);
            }

            inv.Invoice.SameTaxes = taxes.ToArray();
            inv.Invoice.TotVATAmt = taxes.Select(a => a.VATAmt).Sum().Round2();
            inv.Invoice.TotPriceWoVAT = (inv.Invoice.TotPrice - inv.Invoice.TotVATAmt).Round2();
        }

        #endregion            

        return inv;
    }


    public void SignIKOF(RegisterInvoiceRequest inv)
    {
        var cert = _appUser!.LegalEntity.EfiCertData;
        var pass = _appUser!.LegalEntity.EfiPassword;

        var iicInput = "";
        // issuerTIN
        iicInput += "12345678";
        // dateTimeCreated
        iicInput += "|2019-06-12T17:05:43+02:00";
        // invoiceNumber
        iicInput += "|9952";
        // busiUnitCode
        iicInput += "|bb123bb123";
        // tcrCode
        iicInput += "|cc123cc123";
        // softCode
        iicInput += "|ss123ss123";
        // totalPrice
        iicInput += "|99.01";

        var d = inv.Invoice.IssueDateTime;
        var dtm = $"{d.ToString("yyyy")}-{d.ToString("MM")}-{d.ToString("dd")}T{d.ToString("HH")}:{d.ToString("mm")}:{d.ToString("ss")}{d.ToString("zzz")}";

        //iicInput = $"{this.Invoice.Seller.IDNum}|{this.Invoice.IssueDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz")}|{this.Invoice.InvOrdNum}|{this.Invoice.BusinUnitCode}|{this.Invoice.TCRCode}|{this.Invoice.SoftCode}|{this.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}";
        iicInput = $"{inv.Invoice.Seller.IDNum}|{dtm}|{inv.Invoice.InvOrdNum}|{inv.Invoice.BusinUnitCode}|{inv.Invoice.TCRCode}|{inv.Invoice.SoftCode}|{inv.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace(",", "")}";

        X509Certificate2 keyStore = new X509Certificate2(cert, pass);

        // Load a private from a key store
        RSA privateKey = keyStore.GetRSAPrivateKey();

        // Create IIC signature according to RSASSA-PKCS-v1_5            
        byte[] iicSignature = privateKey.SignData(Encoding.ASCII.GetBytes(iicInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string iicSignatureString = BitConverter.ToString(iicSignature).Replace("-", string.Empty);
        //Console.WriteLine("The IIC signature is: " + iicSignatureString);

        inv.Invoice.IICSignature = iicSignatureString;

        // Hash IIC signature with MD5 to create IIC
        byte[] iic = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(iicSignature);
        string iicString = BitConverter.ToString(iic).Replace("-", string.Empty);

        inv.Invoice.IIC = iicString;
    }


    public string SignInvoiceRequest(RegisterInvoiceRequest invoice)
    {
        var cert = _appUser!.LegalEntity.EfiCertData;
        var pass = _appUser!.LegalEntity.EfiPassword;

        using (X509Certificate2 keyStore = new X509Certificate2(cert, pass))
        {
            try
            {
                SignIKOF(invoice);

                var REQUEST_TO_SIGN = invoice.ToXML();

                // Load a private from a key store
                RSA privateKey = keyStore.GetRSAPrivateKey();

                // Convert string XML to object
                XmlDocument request = new XmlDocument();
                request.LoadXml(REQUEST_TO_SIGN);

                // Create key info element
                KeyInfo keyInfo = new KeyInfo();
                KeyInfoX509Data keyInfoData = new KeyInfoX509Data();
                keyInfoData.AddCertificate(keyStore);
                keyInfo.AddClause(keyInfoData);

                // Create signature reference
                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(false));
                reference.AddTransform(new XmlDsigExcC14NTransform(false));
                reference.DigestMethod = XML_DIG_METHOD;
                reference.Uri = "#" + XML_REQUEST_ID;

                // Create signature
                SignedXml xml = new SignedXml(request);
                xml.SigningKey = privateKey;
                xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
                xml.SignedInfo.SignatureMethod = XML_SIG_METHOD;
                xml.KeyInfo = keyInfo;
                xml.AddReference(reference);
                xml.ComputeSignature();
                // Add signature element to the request
                XmlElement signature = xml.GetXml();
                request.DocumentElement.AppendChild(signature);

                invoice.Signature = new SignatureType();

                invoice.Signature.SignedInfo = new SignedInfoType();
                invoice.Signature.SignedInfo.CanonicalizationMethod = new CanonicalizationMethodType();
                invoice.Signature.SignedInfo.CanonicalizationMethod.Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#";
                invoice.Signature.SignedInfo.SignatureMethod = new SignatureMethodType();
                invoice.Signature.SignedInfo.SignatureMethod.Algorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                var Reference = new ReferenceType();
                Reference.URI = "#Request";

                var transform1 = new TransformType();
                transform1.Algorithm = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
                var transform2 = new TransformType();
                transform2.Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#";

                Reference.Transforms = new TransformType[] { transform1, transform2 };

                var sig = signature.OuterXml.ToString();
                var digest = sig.SubstrExcl(sig.IndexOf("<DigestValue>") + 12, sig.IndexOf("</DigestValue>"));
                var prvt = sig.SubstrExcl(sig.IndexOf("<X509Certificate>") + 16, sig.IndexOf("</X509Certificate>"));
                var sigval = sig.SubstrExcl(sig.IndexOf("<SignatureValue>") + 15, sig.IndexOf("</SignatureValue>"));

                Reference.DigestMethod = new DigestMethodType();
                Reference.DigestMethod.Algorithm = "http://www.w3.org/2001/04/xmlenc#sha256";
                Reference.DigestValue = Convert.FromBase64String(digest);

                invoice.Signature.SignedInfo.Reference = new ReferenceType[] { Reference };

                invoice.Signature.SignatureValue = new SignatureValueType();
                invoice.Signature.SignatureValue.Value = Convert.FromBase64String(sigval);

                invoice.Signature.KeyInfo = new KeyInfoType();
                var x509 = new X509DataType();
                x509.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.X509Certificate };
                x509.Items = new object[] { Convert.FromBase64String(prvt) };
                invoice.Signature.KeyInfo.Items = new object[] { x509 };
                invoice.Signature.KeyInfo.ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.X509Data };

                // Convert signed request to string and print
                StringWriter sw = new StringWriter();
                XmlTextWriter xw = new XmlTextWriter(sw);

                request.WriteTo(xw);
                return sw.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return "";
    }


    public string SignCashRequest(RegisterCashDepositRequest cash)
    {
        var cert = _appUser!.LegalEntity.EfiCertData;
        var pass = _appUser.LegalEntity!.EfiPassword;

        using (X509Certificate2 keyStore = new X509Certificate2(cert, pass))
        {
            try
            {
                var REQUEST_TO_SIGN = cash.ToXML();

                // Load a private from a key store
                RSA privateKey = keyStore.GetRSAPrivateKey()!;

                // Convert string XML to object
                XmlDocument request = new XmlDocument();
                request.LoadXml(REQUEST_TO_SIGN);

                // Create key info element
                KeyInfo keyInfo = new KeyInfo();
                KeyInfoX509Data keyInfoData = new KeyInfoX509Data();
                keyInfoData.AddCertificate(keyStore);
                keyInfo.AddClause(keyInfoData);

                // Create signature reference
                Reference reference = new Reference("");
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(false));
                reference.AddTransform(new XmlDsigExcC14NTransform(false));
                reference.DigestMethod = XML_DIG_METHOD;
                reference.Uri = "#" + XML_REQUEST_ID;

                // Create signature
                SignedXml xml = new SignedXml(request);
                xml.SigningKey = privateKey;
                xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
                xml.SignedInfo.SignatureMethod = XML_SIG_METHOD;
                xml.KeyInfo = keyInfo;
                xml.AddReference(reference);
                xml.ComputeSignature();
                // Add signature element to the request
                XmlElement signature = xml.GetXml();
                request.DocumentElement.AppendChild(signature);

                cash.Signature = new SignatureType();

                cash.Signature.SignedInfo = new SignedInfoType();
                cash.Signature.SignedInfo.CanonicalizationMethod = new CanonicalizationMethodType();
                cash.Signature.SignedInfo.CanonicalizationMethod.Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#";
                cash.Signature.SignedInfo.SignatureMethod = new SignatureMethodType();
                cash.Signature.SignedInfo.SignatureMethod.Algorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                var Reference = new ReferenceType();
                Reference.URI = "#Request";

                var transform1 = new TransformType();
                transform1.Algorithm = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
                var transform2 = new TransformType();
                transform2.Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#";

                Reference.Transforms = new TransformType[] { transform1, transform2 };

                var sig = signature.OuterXml.ToString();
                var digest = sig.SubstrExcl(sig.IndexOf("<DigestValue>") + 12, sig.IndexOf("</DigestValue>"));
                var prvt = sig.SubstrExcl(sig.IndexOf("<X509Certificate>") + 16, sig.IndexOf("</X509Certificate>"));
                var sigval = sig.SubstrExcl(sig.IndexOf("<SignatureValue>") + 15, sig.IndexOf("</SignatureValue>"));

                Reference.DigestMethod = new DigestMethodType();
                Reference.DigestMethod.Algorithm = "http://www.w3.org/2001/04/xmlenc#sha256";
                Reference.DigestValue = Convert.FromBase64String(digest);

                cash.Signature.SignedInfo.Reference = new ReferenceType[] { Reference };

                cash.Signature.SignatureValue = new SignatureValueType();
                cash.Signature.SignatureValue.Value = Convert.FromBase64String(sigval);

                cash.Signature.KeyInfo = new KeyInfoType();
                var x509 = new X509DataType();
                x509.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.X509Certificate };
                x509.Items = new object[] { Convert.FromBase64String(prvt) };
                cash.Signature.KeyInfo.Items = new object[] { x509 };
                cash.Signature.KeyInfo.ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.X509Data };

                // Convert signed request to string and print
                StringWriter sw = new StringWriter();
                XmlTextWriter xw = new XmlTextWriter(sw);

                request.WriteTo(xw);
                return sw.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        return null;            
    }


    public string GetFiscalParameter(string parameter, bool test = true)
    {
        if (parameter == "URL" && test == false) return _config["EFI:PROD:URL"]!;
        if (parameter == "URL" && test == true) return _config["EFI:TEST:URL"]!;
        if (parameter == "QR" && test == false) return _config["EFI:PROD:QR"]!;
        if (parameter == "QR" && test == true) return _config["EFI:TEST:QR"]!;
        if (parameter == "SFT" && test == false) return _config["EFI:PROD:SFT"]!;
        if (parameter == "SFT" && test == true) return _config["EFI:TEST:SFT"]!;
        if (parameter == "MNT" && test == false) return _config["EFI:PROD:MNT"]!;
        if (parameter == "MNT" && test == true) return _config["EFI:TEST:MNT"]!;

        return null;
    }
            
    
    public string QrUrl(RegisterInvoiceRequest request)
    {   
        var d = request.Invoice.IssueDateTime;
        var dtm = $"{d.ToString("yyyy")}-{d.ToString("MM")}-{d.ToString("dd")}T{d.ToString("HH")}:{d.ToString("mm")}:{d.ToString("ss")}{d.ToString("zzz")}";

        var qrurl = GetFiscalParameter("QR", _appUser!.LegalEntity.Test);        

        return $@"{qrurl}/ic/#/verify?iic={request.Invoice.IIC}&tin={request.Invoice.Seller.IDNum}&crtd={dtm}&ord={request.Invoice.InvOrdNum}&bu={request.Invoice.BusinUnitCode}&cr={request.Invoice.TCRCode}&sw={request.Invoice.SoftCode}&prc={request.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace(",", "")}";        
    }
    
}