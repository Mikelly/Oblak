using Humanizer;
using Oblak.Data;
using System.ComponentModel.DataAnnotations;

namespace Oblak.Models.Api
{
    public class MnePersonDto
    {
        public int Id { get; set; }
        public int? ExternalId { get; set; }
        public string? Guid { get; set; }
        public int? LegalEntityId { get; set; }
        public string? LegalEntityName { get; set; }
        public int PropertyExternalId { get; set; }
        public int PropertyId { get; set; }
        public int? GroupId { get; set; }
        public int? UnitId { get; set; }
        public string LastName { get; set; } // 
        public string FirstName { get; set; } //
        public string? PersonalNumber { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public bool IsDeleted { get; set; }
        public string? Status { get; set; }
        public string? Error { get; set; }
        public string BirthPlace { get; set; }
        public string BirthCountry { get; set; }
        public string Nationality { get; set; }
        public string PersonType { get; set; } //
        public string PermanentResidenceCountry { get; set; }
        public string PermanentResidencePlace { get; set; }
        public string PermanentResidenceAddress { get; set; }
        public string? ResidencePlace { get; set; }
        public string? ResidenceAddress { get; set; }
        public DateTime CheckIn { get; set; } //
        public DateTime? CheckOut { get; set; } //
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentValidTo { get; set; }
        public string DocumentCountry { get; set; }
        public string DocumentIssuer { get; set; }
        public string? VisaType { get; set; } //
        public string? VisaNumber { get; set; } // 
        public DateTime? VisaValidFrom { get; set; }
        public DateTime? VisaValidTo { get; set; } //
        public string? VisaIssuePlace { get; set; }
        public string? EntryPoint { get; set; }
        public DateTime? EntryPointDate { get; set; }
        public string? Other { get; set; }
		public string? Note { get; set; }
		public int? ResTaxTypeId { get; set; }        
        public int? ResTaxPaymentTypeId { get; set; }
        public int? ResTaxExemptionTypeId { get; set; }
        public int? ResTaxStatus { get; set; }
        public decimal? ResTaxAmount { get; set; }
        public decimal? ResTaxFee { get; set; }
        public string? UserCreated { get; set; }
        public DateTime? UserCreatedDate { get; set; }
        public string? CheckInPointName { get; set; }

        public MnePerson SetMnePerson(MnePerson mnePerson, int legalEntityId)
        {
            mnePerson.LegalEntityId = legalEntityId;
            //mnePerson.Id = dto.Id;
            mnePerson.ExternalId = this.ExternalId;
            //mnePerson.Guid = this.Guid;
            //mnePerson.LegalEntityId = this.LegalEntity.Id;
            //mnePerson.LegalEntityName = this.LegalEntityName;
            //mnePerson.PropertyExternalId = this.PropertyExternalId;
            mnePerson.PropertyId = this.PropertyId;
            mnePerson.GroupId = this.GroupId;
            //mnePerson.UnitId = this.UnitId;
            mnePerson.LastName = this.LastName;
            mnePerson.FirstName = this.FirstName;
            mnePerson.PersonalNumber = this.PersonalNumber;
            mnePerson.Gender = this.Gender;
            mnePerson.BirthDate = this.BirthDate;
            mnePerson.IsDeleted = this.IsDeleted;
            mnePerson.Status = this.Status;
            mnePerson.Error = this.Error;
            mnePerson.BirthPlace = this.BirthPlace;
            mnePerson.BirthCountry = this.BirthCountry;
            mnePerson.Nationality = this.Nationality;
            mnePerson.PersonType = this.PersonType;
            mnePerson.PermanentResidenceCountry = this.PermanentResidenceCountry;
            mnePerson.PermanentResidencePlace = this.PermanentResidencePlace;
            mnePerson.PermanentResidenceAddress = this.PermanentResidenceAddress;
            mnePerson.CheckIn = this.CheckIn;
            mnePerson.CheckOut = this.CheckOut;
            mnePerson.DocumentType = this.DocumentType;
            mnePerson.DocumentNumber = this.DocumentNumber;
            mnePerson.DocumentValidTo = this.DocumentValidTo;
            mnePerson.DocumentCountry = this.DocumentCountry;
            mnePerson.DocumentIssuer = this.DocumentIssuer;
            mnePerson.VisaType = this.VisaType;
            mnePerson.VisaNumber = this.VisaNumber;
            mnePerson.VisaValidFrom = this.VisaValidFrom;
            mnePerson.VisaValidTo = this.VisaValidTo;
            mnePerson.VisaIssuePlace = this.VisaIssuePlace;
            mnePerson.EntryPoint = this.EntryPoint;
            mnePerson.EntryPointDate = this.EntryPointDate;
            mnePerson.Other = this.Other;
            mnePerson.Note = this.Note;
            mnePerson.ResTaxAmount = this.ResTaxAmount;
            mnePerson.ResTaxFee = this.ResTaxFee;
            mnePerson.ResTaxTypeId = this.ResTaxTypeId;
            mnePerson.ResTaxPaymentTypeId = this.ResTaxPaymentTypeId;
            mnePerson.ResTaxExemptionTypeId = this.ResTaxExemptionTypeId;
            //mnePerson.ResTaxStatus = this.ResTaxStatus switch 
            //{
            //    "Unpaid" => Data.Enums.ResTaxPaymentStatus.Unpaid,
            //    "Cash" => Data.Enums.ResTaxPaymentStatus.Cash,
            //    "Card" => Data.Enums.ResTaxPaymentStatus.Card,
            //    "BankAccount" => Data.Enums.ResTaxPaymentStatus.BankAccount,
            //    _ => Data.Enums.ResTaxPaymentStatus.Unpaid
            //};

            return mnePerson;
        }

        public MnePersonEnrichedDto GetFromMnePerson(MnePerson mnePerson)
        {
            var enriched = new MnePersonEnrichedDto()
            {
                Id = mnePerson.Id,
                LegalEntityId = mnePerson.LegalEntity.Id,
                ExternalId = mnePerson.ExternalId,
                Guid = mnePerson.Guid,
                LegalEntityName = mnePerson.LegalEntity.Name,
                PropertyName = mnePerson.Property.Name,
                PropertyExternalId = mnePerson.Property.ExternalId,
                PropertyId = mnePerson.PropertyId,
                GroupId = mnePerson.GroupId,
                LastName = mnePerson.LastName,
                FirstName = mnePerson.FirstName,
                PersonalNumber = mnePerson.PersonalNumber,
                Gender = mnePerson.Gender,
                BirthDate = mnePerson.BirthDate,
                IsDeleted = mnePerson.IsDeleted,
                Status = mnePerson.Status,
                Error = mnePerson.Error,
                BirthPlace = mnePerson.BirthPlace,
                BirthCountry = mnePerson.BirthCountry,
                Nationality = mnePerson.Nationality,
                PersonType = mnePerson.PersonType,
                PermanentResidenceCountry = mnePerson.PermanentResidenceCountry,
                PermanentResidencePlace = mnePerson.PermanentResidencePlace,
                PermanentResidenceAddress = mnePerson.PermanentResidenceAddress,
                CheckIn = mnePerson.CheckIn,
                CheckOut = mnePerson.CheckOut,
                DocumentType = mnePerson.DocumentType,
                DocumentNumber = mnePerson.DocumentNumber,
                DocumentValidTo = mnePerson.DocumentValidTo,
                DocumentCountry = mnePerson.DocumentCountry,
                DocumentIssuer = mnePerson.DocumentIssuer,
                VisaType = mnePerson.VisaType,
                VisaNumber = mnePerson.VisaNumber,
                VisaValidFrom = mnePerson.VisaValidFrom,
                VisaValidTo = mnePerson.VisaValidTo,
                VisaIssuePlace = mnePerson.VisaIssuePlace,
                EntryPoint = mnePerson.EntryPoint,
                EntryPointDate = mnePerson.EntryPointDate,
                Other = mnePerson.Other,
                Note = mnePerson.Note,
                ResTaxAmount = mnePerson.ResTaxAmount,
                ResTaxFee = mnePerson.ResTaxFee,
                ResTaxTypeId = mnePerson.ResTaxTypeId,
                ResTaxPaymentTypeId = mnePerson.ResTaxPaymentTypeId,
                ResTaxExemptionTypeId = mnePerson.ResTaxExemptionTypeId,
                UserCreated = mnePerson.UserCreated,
                UserCreatedDate = mnePerson.UserCreatedDate,
                CheckInPointName = mnePerson.CheckInPoint.Name,
            };

            return enriched;
        }
    }

    public class MnePersonEnrichedDto : MnePersonDto
    {
        public string? FullName { get; set; } //
        public string? PropertyName { get; set; } //
        public string? LegalEntity { get; set; } //
        public bool? Registered { get; set; } = true;
        public bool? Locked { get; set; } = true;
        public bool? Deleted { get; set; } = false;
    }
}