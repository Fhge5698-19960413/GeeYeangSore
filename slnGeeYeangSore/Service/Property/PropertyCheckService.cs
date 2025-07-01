using GeeYeangSore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Property
{
    public class PropertyCheckService : IPropertyCheckService
    {
        private readonly GeeYeangSoreContext _context;

        public PropertyCheckService(GeeYeangSoreContext context)
        {
            this._context = context;
        }

        public async Task<List<HPropertyAudit>> GetPropertyAuditListByAsync(string sortOrder, string searchString, string searchType)
        {
            var audits = _context.HPropertyAudits
                    .Include(a => a.HLandlord)
                    .Include(a => a.HProperty)
                    .Where(a => (a.HIsDelete == null || a.HIsDelete == false))  // 只篩選未被刪除的審核記錄
                    .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                switch (searchType?.ToLower())
                {
                    case "id":
                        if (int.TryParse(searchString, out int auditId))
                        {
                            audits = audits.Where(a => a.HAuditId == auditId);
                        }
                        break;
                    case "propertyid":
                        if (int.TryParse(searchString, out int propertyId))
                        {
                            audits = audits.Where(a => a.HPropertyId == propertyId);
                        }
                        break;
                    case "landlordid":
                        if (int.TryParse(searchString, out int landlordId))
                        {
                            audits = audits.Where(a => a.HLandlordId == landlordId);
                        }
                        break;
                    case "auditorid":
                        if (int.TryParse(searchString, out int auditorId))
                        {
                            audits = audits.Where(a => a.HAuditorId == auditorId);
                        }
                        break;
                    case "status":
                        audits = audits.Where(a => a.HAuditStatus.Contains(searchString));
                        break;
                    case "propertytitle":
                        audits = audits.Where(a => a.HProperty.HPropertyTitle.Contains(searchString));
                        break;
                    case "landlordname":
                        audits = audits.Where(a => a.HLandlord.HLandlordName.Contains(searchString));
                        break;
                    default: // "all" or any other value
                        audits = audits.Where(a =>
                            a.HAuditId.ToString().Contains(searchString) ||
                            a.HPropertyId.ToString().Contains(searchString) ||
                            a.HLandlordId.ToString().Contains(searchString) ||
                            a.HAuditorId.ToString().Contains(searchString) ||
                            a.HAuditStatus.Contains(searchString) ||
                            a.HAuditNotes.Contains(searchString) ||
                            a.HProperty.HPropertyTitle.Contains(searchString) ||
                            a.HLandlord.HLandlordName.Contains(searchString));
                        break;
                }
            }

            // 根據排序參數進行排序
            switch (sortOrder)
            {
                case "id_desc":
                    audits = audits.OrderByDescending(a => a.HAuditId);
                    break;
                case "id":
                    audits = audits.OrderBy(a => a.HAuditId);
                    break;
                case "propertyid_desc":
                    audits = audits.OrderByDescending(a => a.HPropertyId);
                    break;
                case "propertyid":
                    audits = audits.OrderBy(a => a.HPropertyId);
                    break;
                case "landlordid_desc":
                    audits = audits.OrderByDescending(a => a.HLandlordId);
                    break;
                case "landlordid":
                    audits = audits.OrderBy(a => a.HLandlordId);
                    break;
                case "status_desc":
                    audits = audits.OrderByDescending(a => a.HAuditStatus);
                    break;
                case "status":
                    audits = audits.OrderBy(a => a.HAuditStatus);
                    break;
                case "auditdate":
                    audits = audits.OrderBy(a => a.HAuditDate);
                    break;
                case "auditdate_desc":
                    audits = audits.OrderByDescending(a => a.HAuditDate);
                    break;
                case "propertytitle_desc":
                    audits = audits.OrderByDescending(a => a.HProperty.HPropertyTitle);
                    break;
                case "propertytitle":
                    audits = audits.OrderBy(a => a.HProperty.HPropertyTitle);
                    break;
                case "landlordname_desc":
                    audits = audits.OrderByDescending(a => a.HLandlord.HLandlordName);
                    break;
                case "landlordname":
                    audits = audits.OrderBy(a => a.HLandlord.HLandlordName);
                    break;
                default: // "id" or empty
                    audits = audits.OrderBy(a => a.HAuditId);
                    break;
            }
            return await audits.ToListAsync();
        }

        public async Task<HPropertyAudit> GetPropertyAuditByIdAsync(int auditId)
        {
            return await _context.HPropertyAudits
                .Include(a => a.HLandlord)
                .Include(a => a.HProperty)
                .FirstOrDefaultAsync(a => a.HAuditId == auditId);
        }

        public async Task<List<HPropertyAudit>> GetPropertyAuditsByPropertyIdAsync(int propertyId)
        {
            return await _context.HPropertyAudits
                .Include(a => a.HLandlord)
                .Include(a => a.HProperty)
                .Where(a => a.HPropertyId == propertyId && (a.HIsDelete == null || a.HIsDelete == false))
                .OrderByDescending(a => a.HAuditDate)
                .ToListAsync();
        }

        public async Task<List<HPropertyAudit>> GetPropertyAuditsByLandlordIdAsync(int landlordId)
        {
            return await _context.HPropertyAudits
                .Include(a => a.HLandlord)
                .Include(a => a.HProperty)
                .Where(a => a.HLandlordId == landlordId && (a.HIsDelete == null || a.HIsDelete == false))
                .OrderByDescending(a => a.HAuditDate)
                .ToListAsync();
        }

        public async Task<List<HPropertyAudit>> GetPropertyAuditsByStatusAsync(string status)
        {
            return await _context.HPropertyAudits
                .Include(a => a.HLandlord)
                .Include(a => a.HProperty)
                .Where(a => a.HAuditStatus == status && (a.HIsDelete == null || a.HIsDelete == false))
                .OrderByDescending(a => a.HAuditDate)
                .ToListAsync();
        }

        public async Task<bool> CreatePropertyAuditAsync(HPropertyAudit propertyAudit)
        {
            try
            {
                propertyAudit.HAuditDate = DateTime.Now;
                _context.HPropertyAudits.Add(propertyAudit);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdatePropertyAuditAsync(HPropertyAudit propertyAudit)
        {
            try
            {
                var existingAudit = await _context.HPropertyAudits.FindAsync(propertyAudit.HAuditId);
                if (existingAudit == null)
                    return false;

                existingAudit.HPropertyId = propertyAudit.HPropertyId;
                existingAudit.HLandlordId = propertyAudit.HLandlordId;
                existingAudit.HAuditorId = propertyAudit.HAuditorId;
                existingAudit.HAuditStatus = propertyAudit.HAuditStatus;
                existingAudit.HAuditNotes = propertyAudit.HAuditNotes;
                existingAudit.HAuditDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeletePropertyAuditAsync(int auditId)
        {
            try
            {
                var audit = await _context.HPropertyAudits.FindAsync(auditId);
                if (audit == null)
                    return false;

                audit.HIsDelete = true;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ApprovePropertyAsync(int propertyId, int auditorId, string auditNotes = null)
        {
            try
            {
                // 更新房源狀態為已驗證
                var property = await _context.HProperties.FindAsync(propertyId);
                if (property == null)
                    return false;

                property.HStatus = "已驗證";
                property.HLastUpdated = DateTime.Now;

                // 創建審核記錄
                var propertyAudit = new HPropertyAudit
                {
                    HPropertyId = propertyId,
                    HLandlordId = property.HLandlordId,
                    HAuditorId = auditorId,
                    HAuditStatus = "已驗證",
                    HAuditDate = DateTime.Now,
                    HAuditNotes = auditNotes ?? "已驗證"
                };

                _context.HPropertyAudits.Add(propertyAudit);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RejectPropertyAsync(int propertyId, int auditorId, string auditNotes = null)
        {
            try
            {
                // 更新房源狀態為已拒絕
                var property = await _context.HProperties.FindAsync(propertyId);
                if (property == null)
                    return false;

                property.HStatus = "已拒絕";
                property.HLastUpdated = DateTime.Now;

                // 創建審核記錄
                var propertyAudit = new HPropertyAudit
                {
                    HPropertyId = propertyId,
                    HLandlordId = property.HLandlordId,
                    HAuditorId = auditorId,
                    HAuditStatus = "已拒絕",
                    HAuditDate = DateTime.Now,
                    HAuditNotes = auditNotes ?? "已拒絕"
                };

                _context.HPropertyAudits.Add(propertyAudit);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
