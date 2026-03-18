using System;
using System.Collections.Generic;
using System.Text;

namespace RegistryParser.Models.DTOs
{
    /// <summary>
    /// DTO объект плательщика
    /// Содержит все поля для хранения информации о плательщике
    /// </summary>
    public partial class INREG_PAYER
    {
        public INREG_PAYER()
        {
            this.INREG_COUNTER = new HashSet<INREG_COUNTER>();
            this.INREG_PAYER1 = new HashSet<INREG_PAYER>();
            this.INREG_SERVICE = new HashSet<INREG_SERVICE>();
        }

        public long ID { get; set; }
        public long IRP_CODE_REG { get; set; }
        public int IRP_PARTNER_ID { get; set; }
        public string IRP_PAYER_UID { get; set; }
        public string IRP_PAYER_FCNUMBER { get; set; }
        public string IRP_PAYER_FCADDNUMBER { get; set; }
        public string IRP_PAYER_FIRSTNAME { get; set; }
        public string IRP_PAYER_LASTNAME { get; set; }
        public string IRP_PAYER_MIDDLENAME { get; set; }
        public string IRP_PAYER_FULLNAME { get; set; }
        public string IRP_PAYER_ADDRESS { get; set; }
        public int? IRP_PAYER_INDEX { get; set; }
        public string IRP_PAYER_REGION { get; set; }
        public string IRP_PAYER_TOWN { get; set; }
        public string IRP_PAYER_STREET { get; set; }
        public string IRP_PAYER_STREET_TYPE { get; set; }
        public string IRP_PAYER_HOUSE { get; set; }
        public string IRP_PAYER_HOUSE_LIT { get; set; }
        public string IRP_PAYER_CORPUS { get; set; }
        public string IRP_PAYER_APARTMENT { get; set; }
        public string IRP_PAYER_APT_LIT { get; set; }
        public string IRP_PAYER_PHONE { get; set; }
        public decimal IRP_PAYSUMMA { get; set; }
        public decimal? IRP_FINESUMMA { get; set; }
        public decimal IRP_FULLSUMMA { get; set; }
        public string IRP_DEPARTMENT { get; set; }
        public int? IRP_OPMONTH { get; set; }
        public int? IRP_OPYEAR { get; set; }
        public string IRP_PAYER_FCOLDNUMBER { get; set; }
        public string IRP_UNIFIELD1 { get; set; }
        public string IRP_UNIFIELD2 { get; set; }
        public string IRP_PAYER_INN { get; set; }
        public string IRP_PARTNER_BANKACCOUNT { get; set; }
        public string IRP_PARTNER_BANKBIK { get; set; }
        public long? IRP_CODE_MAINPAYER { get; set; }
        public string IRP_PAYDOC_NUMBER { get; set; }
        public DateTime? IRP_PAYDOC_DATE { get; set; }
        public short? IRP_CHK_FORBID { get; set; }
        public string IRP_COMPANY_NUMBER { get; set; }
        public string IRP_KBK { get; set; }
        public string IRP_INTEGRITY { get; set; }
        public string IRP_PROVIDER_NAME { get; set; }
        public string IRP_PARTNER_OWNERIDSTR { get; set; }
        public string IRP_UNIACCNUMBER { get; set; }
        public string IRP_SCHOOL_NUMBER { get; set; }
        public string IRP_SCHOOL_CLASS { get; set; }
        public int? IRP_QUANTITY { get; set; }
        public string IRP_CHILDNAME { get; set; }
        public int? FIELD_1 { get; set; }
        public int? FIELD_2 { get; set; }
        public decimal? FIELD_3 { get; set; }
        public decimal? FIELD_4 { get; set; }
        public int? FIELD_5 { get; set; }
        public decimal? FIELD_6 { get; set; }
        public decimal? FIELD_7 { get; set; }
        public decimal? FIELD_8 { get; set; }
        public decimal? FIELD_9 { get; set; }
        public decimal? FIELD_10 { get; set; }
        public string IRP_NORM_PAYER_TOWN { get; set; }
        public string IRP_NORM_PAYER_STREET { get; set; }
        public string IRP_NORM_PAYER_HOUSE { get; set; }
        public string IRP_NORM_PAYER_AP { get; set; }
        public string IRP_FIAS_GUID { get; set; }
        public DateTime? IRP_DATE_REVISION_FIAS { get; set; }
        public string IRP_NORM_PAYER_ADDRESS { get; set; }
        public string IRP_FIAS_STREET_GUID { get; set; }

        public virtual ICollection<INREG_COUNTER> INREG_COUNTER { get; set; }
        public virtual ICollection<INREG_PAYER> INREG_PAYER1 { get; set; }
        public virtual ICollection<INREG_SERVICE> INREG_SERVICE { get; set; }
        public virtual INREG_PAYER INREG_PAYER2 { get; set; }
    }
}
