using EfiService;
using Oblak.Data;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography;
using System.Xml;
using System.Text;
using Oblak.Models.rb90;
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
        if (doc.Status == "F") throw new Exception("Račun je već fiskalizovan!");

        var enu = _db.FiscalEnu.FirstOrDefault(a => a.Code == doc.FiscalEnuCode)!;

        if (doc.TypeOfInvoce == "CASH" && enu.AutoDeposit.HasValue)
        {
            var deposit = _db.FiscalRequests.Any(a => a.FiscalEnuCode == doc.FiscalEnuCode && a.FicalizationDate.Date == doc.InvoiceDate.Date && a.RequestType == Data.Enums.FiscalRequestType.RegisterCashDeposit && a.FCDC != null);
            if (deposit == false)
            {
                var amount = enu.AutoDeposit.Value;
                await Cash(amount, "i", doc.FiscalEnuCode, doc.BusinessUnitCode);
            }
        }

        var url = GetFiscalParameter("URL");

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
            //fr.Company = f;                
            //fr.FiscalEnu = doc.FiscalEnuCode;
            fr.FiscalEnuCode = doc.FiscalEnuCode;
            //fr.BusinessUnit = doc.BusinessUnitCode;
            fr.BusinessUnitCode = doc.BusinessUnitCode;
            fr.Amount = request.Invoice.TotPrice;
            fr.FicalizationDate = DateTime.Now;
            fr.Status = "A";
            doc.Status = "A";
            //doc.IntProperty5 = 2; // Neuspješno fiskalizovan;
            doc.IIC = request.Invoice.IIC;                

            fr.Request = xml;            
            fr.IIC = request.Invoice.IIC;
            //doc.QR = request.URL;
            
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(doc.Qr, QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(3, SixLabors.ImageSharp.Color.Black, SixLabors.ImageSharp.Color.White, false);
            //Bitmap qrCodeImage = qrCode.GetGraphic(3, Color.Black, Color.White, false);
            //Bitmap resized = new Bitmap(qrCodeImage, new Size(qrCodeImage.Width / 7, qrCodeImage.Height / 7));
            var path = Path.Combine(_env.WebRootPath, $"~/QR/{doc.Id}.bmp");
            doc.QrPath = path;
            _db.SaveChanges();
            //qrCodeImage = qrCodeImage.Clone(new Rectangle(0, 0, qrCodeImage.Width, qrCodeImage.Height), PixelFormat.Format1bppIndexed);
            //qrCodeImage.Save(path, ImageFormat.Bmp);

            response = client.registerInvoice(request);
                        
            fr.Response = response.ToXML();
            fr.FIC = response.FIC.ToUpper().Replace("-", "");
            fr.Status = "F";
            doc.FIC = fr.FIC;
            doc.Status = "F";
            doc.FIC = fr.FIC;                
            _db.SaveChanges();
        }
        catch (Exception excp)
        {
            _logger.LogDebug("fiscal error: " + excp.Message);

            string error = "";

            if (excp is System.ServiceModel.FaultException)
            {
                error = Exceptions.ParseException(excp);
                doc.Status = "A";
                throw new Exception(error);
            }
            else
            {
                error = Exceptions.StringException(excp);
                doc.Status = "N";
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
        var f = _context.Session.GetInt32("FIRMA");        

        var url = GetFiscalParameter("URL");

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
        cash.CashDeposit.ChangeDateTime = DateTime.Now.forXML();

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
        inv.Invoice = new InvoiceType();

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
        inv.Invoice.InvOrdNum = doc.No;
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
                    
        var oper = _appUser.EfiOperator;
        if ((oper ?? "") == "")
        {
            throw new Exception("Neispravno definisan operator!");
        }

        inv.Invoice.OperatorCode = oper;            
        inv.Invoice.SoftCode = GetFiscalParameter("SFT");
        inv.Invoice.IsReverseCharge = false;

        inv.Invoice.Currency = new CurrencyType();
        inv.Invoice.Currency.Code = doc.CurrencyCode == "USD" ? CurrencyCodeSType.USD : doc.CurrencyCode == "GBP" ? CurrencyCodeSType.GBP : CurrencyCodeSType.EUR;
        inv.Invoice.Currency.ExRate = (double)(doc.ExchangeRate == 0 ? 1 : doc.ExchangeRate);

        inv.Invoice.Seller = new SellerType();
        inv.Invoice.Seller.IDType = IDTypeSType.TIN;
        inv.Invoice.Seller.IDNum = _appUser.LegalEntity.TIN;
        inv.Invoice.Seller.Name = _appUser.LegalEntity.Name;
        inv.Invoice.Seller.Address = _appUser.LegalEntity.Address;
        
        inv.Invoice.Buyer = new BuyerType();
        if (doc.PartnerIdNumber != null && doc.PartnerIdType != null && doc.PartnerName != null)
        {
            inv.Invoice.Buyer.IDType = doc.PartnerIdType switch
            { 
                "ID" => IDTypeSType.ID,
                "PASS" => IDTypeSType.PASS,
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
                    1 => PaymentMethodTypeSType.BANKNOTE,
                    2 => PaymentMethodTypeSType.ACCOUNT,
                    3 => PaymentMethodTypeSType.CARD,
                    4 => PaymentMethodTypeSType.ADVANCE,
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
                    "VAT_CL17" => ExemptFromVATSType.VAT_CL17,
                    "VAT_CL20" => ExemptFromVATSType.VAT_CL20,
                    "VAT_CL26" => ExemptFromVATSType.VAT_CL26,
                    "VAT_CL27" => ExemptFromVATSType.VAT_CL27,
                    "VAT_CL28" => ExemptFromVATSType.VAT_CL28,
                    "VAT_CL29" => ExemptFromVATSType.VAT_CL29,
                    "VAT_CL30" => ExemptFromVATSType.VAT_CL30,
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

        X509Certificate2 keyStore = new X509Certificate2(cert);

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

        using (X509Certificate2 keyStore = new X509Certificate2(cert))
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

        using (X509Certificate2 keyStore = new X509Certificate2(cert))
        {
            try
            {
                var REQUEST_TO_SIGN = this.ToXML();

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


    public string GetFiscalParameter(string parameter, bool test = false)
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

        var qrurl = GetFiscalParameter("QR");        

        return $@"{qrurl}/ic/#/verify?iic={request.Invoice.IIC}&tin={request.Invoice.Seller.IDNum}&crtd={dtm}&ord={request.Invoice.InvOrdNum}&bu={request.Invoice.BusinUnitCode}&cr={request.Invoice.TCRCode}&sw={request.Invoice.SoftCode}&prc={request.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace(",", "")}";        
    }

    /*
    public Document Storno(Document doc)
    {
        return doc;
    }


    public Document CreateInvoice(int? g, int? o, int? gost, int? noc)
    {   
        Item brv = null;
        Item btx = null;
        CheckItems(out brv, out btx);

        List<Stavka> stavke = new List<Stavka>();
        
        var obj = _db.rb90Properties.FirstOrDefault(a => a.Id == o.Value);
        var npl = obj.PaymentType;
        var cij = obj.Price ?? 0m;
        var bor = (obj.ResidenceTaxYN ?? true) ? (obj.ResidenceTax ?? 0m) : 0m;

        stavke.Add(
            new Stavka() { 
                Artikal = brv.Id, 
                Cijena = cij, 
                Kolicina = (npl == "A" ? noc.Value : noc.Value * gost.Value) 
            });

        if (bor > 0)
            stavke.Add(
                new Stavka()
                {
                    Artikal = btx.Id,
                    Cijena = bor,
                    Kolicina = noc.Value * gost.Value
                });                
       
        var doc = new Document();            
        doc.No = 0;
        doc.Status = "A";
        doc.CurrencyCode = "EUR";
        doc.ExchangeRate = 1;
        obj.BusinessUnitCode = obj.BusinessUnitCode;
        doc.FiscalEnuCode = obj.FiscalEnuCode; 
        doc.InvoiceDate = DateTime.Now;
        doc.rb90GroupId = g;
        doc.TypeOfInvoce = "CASH";
        _db.Documents.Add(doc);
        _db.SaveChanges();
        doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
        _db.SaveChanges();            
        

        foreach (var s in doc.DocumentItems.ToList())
        {
            _db.DocumentItems.Remove(s);
        }
        _db.SaveChanges();

        foreach (var i in stavke)
        {
            var art = _db.Items.Where(a => a.Id == i.Artikal).FirstOrDefault();

            var stavka = new DocumentItem();

            stavka.DocumentId = doc.Id;
            stavka.ItemId = art.Id;
            stavka.ItemUnit = art.Unit;
            stavka.Quantity = i.Kolicina;
            stavka.UnitPrice = i.Cijena;
            stavka.Discount = 0;
            stavka.VatRate = art.VatRate;
            stavka.VatExempt = art.VatExempt;

            _db.DocumentItems.Add(stavka);
            _db.SaveChanges();

            SetItemValues(stavka);

            _db.SaveChanges();
        }

        Payment(doc, null);

        return doc;
    }


    public (Document, string, string) CreateRacun(Racun racun, BusinessUnit bu, FiscalEnu enu)
    {
        var doc = _db.Documents.Where(a => a.Id == racun!.ID).FirstOrDefault();
        if (doc == null)
        {
            doc = new Document();

            doc.Status = "A";
            _db.SaveChanges();
            doc.IdEncrypted = Encryptor.Base64Encode(Encryptor.EncryptSimple(doc.Id.ToString()));
            _db.SaveChanges();
        }
        else
        {
            if (doc.Status == "F")
            {
                return (doc, "", "Ne možete mijenjati račun koji je fiskalizovan");                                        
            }
        }

        doc.No = 0;
        doc.BusinessUnitCode = bu.Code;
        doc.FiscalEnuCode = enu.Code;
        doc.InvoiceDate = racun.Datum;
        doc.rb90GroupId = racun.PrijavaID;
        doc.TypeOfInvoce = "CASH";
        _db.SaveChanges();

        var to_delete = doc.DocumentItems.Where(a => racun.stavke.Any(b => b.ID != 0 && b.ID == a.Id) == false).ToList();
        foreach (var del in to_delete) _db.DocumentItems.Remove(del);
        _db.SaveChanges();

        foreach (var stavka in racun.stavke)
        {
            var s = UpdateStavka(doc, stavka, "N");
        }

        Payment(doc, racun.NacinPlacanja);

        var ime = racun.Kupac ?? "";
        var brd = racun.Dokument ?? "";
        var pib = racun.PIB ?? "";
        var vrs = racun.VrstaKupca;
        var vrd = racun.VrstaDokumenta;

        doc.PartnerName = racun.Kupac;
        doc.PartnerType = racun.VrstaKupca;
        doc.PartnerIdType = racun.VrstaKupca;
        doc.PartnerIdNumber = racun.Dokument;

        _db.SaveChanges();

        var rac = Doc2Racun(doc);

        return (doc, "", "");
    }


    public void DeleteRacun(Document doc)
    {
        return;
    }


    public DocumentItem UpdateStavka(Document doc, Stavka s, string delete)
    {
        DocumentItem sd = null;
        if (s.ID == 0)
        {
            sd = new DocumentItem();
            sd.DocumentId = doc.Id;
            doc.DocumentItems.Add(sd);
        }
        else
        {
            sd = _db.DocumentItems.Where(a => a.Id == s.ID).FirstOrDefault();
        }

        if (delete == "Y")
        {
            _db.DocumentItems.Remove(sd);
            _db.SaveChanges();

            return new DocumentItem();
        }

        var art = _db.Items.Where(a => a.Id == s.Artikal).FirstOrDefault();

        sd.DocumentId = doc.Id;
        sd.ItemId = art.Id;
        sd.ItemUnit = art.Unit;
        sd.ItemName = art.Name;
        sd.Quantity = s.Kolicina;
        sd.UnitPrice = s.Cijena;
        sd.Discount = 0;
        sd.VatRate = art.VatRate;
        _db.SaveChanges();

        return sd;
    }


    public void SetItemValues(DocumentItem s)    {
        s.ItemUnit = s.Item.Unit;        
        s.UnitPriceWoVat = s.UnitPrice * 100m / (100m + s.VatRate);        
        s.LineAmount = s.Quantity * s.UnitPrice;
        s.LineTotalWoVat = s.Quantity * s.FinalPrice * 100m / (100m + s.VatRate);
        s.VatAmount = s.Quantity * s.FinalPrice * s.VatRate / (100m + s.VatRate);
        s.LineTotal = s.Quantity * s.FinalPrice;
    }

    public void Payment(Document doc, int? p)
    {
        var payment = doc.DocumentPayments.FirstOrDefault();
        if (payment == null)
        {
            payment = new DocumentPayment();
            payment.DocumentId = doc.Id;                
            payment.PaymentType = p ?? 2;
            payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
            _db.DocumentPayments.Add(payment);
            _db.SaveChanges();
        }
        else
        {
            payment.Amount = doc.DocumentItems.Select(a => a.LineTotal).Sum().Round2();
            if (p != null) payment.PaymentType = p.Value;
            _db.SaveChanges();
        }
    }


    public Racun Doc2Racun(Document d)
    {
        var r = new Racun();
        r.ID = d.Id;
        r.Datum = d.InvoiceDate;
        r.PrijavaID = d.rb90GroupId;
        r.BrojGostiju = 0;
        r.BrojNocenja = 0;
        r.ENU = d.FiscalEnuCode;
        r.Status = d.Status;
        r.Kupac = d.PartnerName;
        r.Dokument = d.PartnerIdNumber;
        r.VrstaKupca = d.PartnerType;
        r.VrstaDokumenta = d.PartnerIdType;
        r.PIB = "";                        

        var pay = d.DocumentPayments.ToList();

        if (pay.Any())
        {
            r.Iznos = pay.Sum(a => a.Amount);
            r.NacinPlacanja = pay.First().PaymentType;
        }
        else
        {
            r.Iznos = 0;
            r.NacinPlacanja = 0;
        }

        r.stavke = new List<Stavka>();

        foreach (var s in d.DocumentItems.ToList())
        {
            r.stavke.Add(new Stavka()
            {
                ID = s.Id,
                RacunID = s.DocumentId,
                Artikal = s.ItemId,
                JedinicaMjere = s.ItemUnit,
                Cijena = s.FinalPrice,
                Kolicina = s.Quantity,
                Iznos = s.LineTotal
            });
        }

        return r;
    }


    public void CheckItems(out Item boravak, out Item btax)
    {
        boravak = _db.Items.Where(a => a.Code == "BORAV").Where(a => a.UserId == _appUser.Id).FirstOrDefault();
        if (boravak == null)
        {
            boravak = new Item();
            boravak.UserId = _appUser.Id;
            boravak.Code = "BORAV";
            boravak.Name = "Usluga smještaja";
            boravak.Unit = "KOM";
            boravak.VatRate = _appUser!.LegalEntity.InVat ? 21m : 0m;            
            _db.Items.Add(boravak);
            _db.SaveChanges();           
        }

        btax = _db.Items.Where(a => a.Code == "BTAX").Where(a => a.UserId == _appUser.Id).FirstOrDefault();        
        if (btax == null)
        {
            btax = new Item();
            btax.UserId = _appUser.Id;
            btax.Code = "BTAX";
            btax.Name = "Boravišna taksa";
            btax.Unit = "KOM";
            btax.VatRate = _appUser.LegalEntity.InVat ? 21m : 0m;
            btax.VatExempt = "CL_20";
            _db.Items.Add(boravak);
            _db.SaveChanges();
        }
    }

    public (int, int) DocumentNumbers(string bu)
    {
        var no = _db.Documents.Where(a => a.BusinessUnitCode == bu).Select(a => (int?)a.No).Max() ?? 0;
        var ordno = _db.Documents.Where(a => a.BusinessUnitCode == bu).Select(a => (int?)a.OrdinalNo).Max() ?? 0;

        return (no + 1, ordno + 1);
    }

    public string ExternalNumber(Document doc)
    {
        return "";
    }

    public async Task<Stream> InvoicePdf(int id)
    {
        var docxProvider = new DocxFormatProvider();
        var pdfProvider = new PdfFormatProvider();
        RadFlowDocument document;

        var path = _webHostEnvironment.WebRootPath + "/templates/invoice.docx";

        using (Stream input = File.OpenRead(path))
        {
            document = docxProvider.Import(input);
        }

        RadFlowDocumentEditor editor = new RadFlowDocumentEditor(document);
        editor.ReplaceText("Point", "trtvrt");
        editor.ReplaceText("<Expr3>", "");

        var stream = new MemoryStream();
        pdfProvider.Export(document, stream);
        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }

    public async Task InvoiceEmail(int id, string email)
    {
        var tax = _db.rb90ResTaxes.FirstOrDefault(a => a.Id == id);
        var obj = _db.rb90Properties.FirstOrDefault(a => a.Id == tax.PropertyId);
        var pdfStream = await InvoicePdf(id);
        var template = _config["SendGrid:Templates:EfiInvoice"]!;
        await _eMailService.SendMail(_appUser.Email, email, template, new
        {
            subject = $@"donotreply: Prijava boravišne takse",
            body = $"Poštovani,\n\nU prilogu se nalazi prijava boravišne takse za period od {tax.DateFrom.ToString("dd.MM.yyyy")} od {tax.DateTo.ToString("dd.MM.yyyy")} za smještajni objekat {obj.Name}.\n\nSrdačan pozdrav,"
        }, ("Boravišna taksa.pdf", pdfStream));
    }

    */
}