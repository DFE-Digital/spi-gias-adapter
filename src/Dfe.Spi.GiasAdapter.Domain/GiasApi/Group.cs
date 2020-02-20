using System;

namespace Dfe.Spi.GiasAdapter.Domain.GiasApi
{
    public class Group
    {
        public long Uid { get; set; }
        public string GroupName { get; set; }
        public string CompaniesHouseNumber { get; set; }
        public string GroupType { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string Status { get; set; }
        public string GroupContactStreet { get; set; }
        public string GroupContactLocality { get; set; }
        public string GroupContactAddress3 { get; set; }
        public string GroupContactTown { get; set; }
        public string GroupContactCounty { get; set; }
        public string GroupContactPostcode { get; set; }
        public string HeadOfGroupTitle { get; set; }
        public string HeadOfGroupFirstName { get; set; }
        public string HeadOfGroupLastName { get; set; }
    }
}