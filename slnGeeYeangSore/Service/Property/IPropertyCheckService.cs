using GeeYeangSore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Property
{
    public interface IPropertyCheckService
    {
        Task<List<HPropertyAudit>> GetPropertyAuditListByAsync(string sortOrder, string searchString, string searchType);
        Task<HPropertyAudit> GetPropertyAuditByIdAsync(int auditId);
        Task<List<HPropertyAudit>> GetPropertyAuditsByPropertyIdAsync(int propertyId);
        Task<List<HPropertyAudit>> GetPropertyAuditsByLandlordIdAsync(int landlordId);
        Task<List<HPropertyAudit>> GetPropertyAuditsByStatusAsync(string status);
        Task<bool> CreatePropertyAuditAsync(HPropertyAudit propertyAudit);
        Task<bool> UpdatePropertyAuditAsync(HPropertyAudit propertyAudit);
        Task<bool> DeletePropertyAuditAsync(int auditId);
        Task<bool> ApprovePropertyAsync(int propertyId, int auditorId, string auditNotes = null);
        Task<bool> RejectPropertyAsync(int propertyId, int auditorId, string auditNotes = null);
    }
}
