namespace Oblak.Models.rb90
{
    public class rb_GrupaVM
    {
        public int ID { get; set; }
        public int FirmaID { get; set; }
        public int ObjekatID { get; set; }
        public string GUID { get; set; }
        public Nullable<int> JedinicaID { get; set; }
        public System.DateTime Datum { get; set; }
        public Nullable<System.DateTime> Dolazak { get; set; }
        public Nullable<System.DateTime> Odlazak { get; set; }
        public string Email { get; set; }
        public Nullable<System.DateTime> DatumPrijave { get; set; }
        public Nullable<System.DateTime> DatumOdjave { get; set; }
        public string Opis { get; set; }
        public string Napomena { get; set; }
        public string Status { get; set; }
    }
}
