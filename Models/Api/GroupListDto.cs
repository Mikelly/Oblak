﻿namespace Oblak.Models.Api
{
    public class GroupListDto
    {
        public int Id { get; set; }
        public int LegalEntityId { get; set; }
        public int PropertyId { get; set; }
        public int? UnitId { get; set; }
        public string GUID { get; set; }        
        public DateTime Date { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }        
        public string Email { get; set; }        
        public string Status { get; set; }
    }
}
