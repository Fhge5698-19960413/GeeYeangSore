using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GeeYeangSore.Models;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using X.PagedList.Extensions;
using System.IO;
using GeeYeangSore.Controllers;
using System;
using Service.Property;

namespace GeeYeangSore.Areas.Admin.Controllers.PropertyCheck
{
    // GET:https://localhost:7022/Admin/PropertyCheck/Index
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class PropertyCheckController : SuperController
    {
        private readonly IPropertyCheckService _propertyCheckService;
        private readonly GeeYeangSoreContext _context;

        public PropertyCheckController(IPropertyCheckService propertyCheckService, GeeYeangSoreContext context)
        {
            _propertyCheckService = propertyCheckService;
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, string searchType = "all", string searchString = "", string sortOrder = "")
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            int pageSize = 15; // 每頁15筆資料

            // 設置排序參數
            ViewData["IdSort"] = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewData["TitleSort"] = sortOrder == "title" ? "title_desc" : "title";
            ViewData["LandlordSort"] = sortOrder == "landlord" ? "landlord_desc" : "landlord";
            ViewData["AddressSort"] = sortOrder == "address" ? "address_desc" : "address";
            ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentSort"] = sortOrder;

            // 使用服務層取得未驗證的房源
            var properties = await _propertyCheckService.GetPropertyAuditListByAsync(sortOrder, searchString, searchType);
            
            // 篩選未驗證的房源
            var unverifiedProperties = properties
                .Where(p => p.HProperty.HStatus == "未驗證" && (p.HProperty.HIsDelete == null || p.HProperty.HIsDelete == false))
                .Select(p => p.HProperty)
                .Distinct()
                .ToList();

            ViewData["CurrentFilter"] = searchString;
            ViewData["SearchType"] = searchType;

            return View(unverifiedProperties.ToPagedList(page, pageSize));
        }

        // GET: Admin/PropertyCheck/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .FirstOrDefaultAsync(m => m.HPropertyId == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // POST: Admin/PropertyCheck/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            try
            {
                // 假設審核員ID為1，實際應用中應該從當前登入用戶取得
                int auditorId = 1;
                var result = await _propertyCheckService.ApprovePropertyAsync(id, auditorId, "已驗證");
                
                if (result)
                {
                    TempData["SuccessMessage"] = "房源已成功核准！";
                }
                else
                {
                    TempData["ErrorMessage"] = "核准房源時發生錯誤";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"核准房源時發生錯誤：{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PropertyCheck/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            try
            {
                // 假設審核員ID為1，實際應用中應該從當前登入用戶取得
                int auditorId = 1;
                var result = await _propertyCheckService.RejectPropertyAsync(id, auditorId, "已拒絕");
                
                if (result)
                {
                    TempData["SuccessMessage"] = "房源已成功拒絕！";
                }
                else
                {
                    TempData["ErrorMessage"] = "拒絕房源時發生錯誤";
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"拒絕房源時發生錯誤：{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/PropertyCheck/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            try
            {
                // 開始交易
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 獲取房源及其相關資料
                        var property = await _context.HProperties
                            .Include(p => p.HPropertyImages)
                            .Include(p => p.HPropertyFeatures)
                            .Include(p => p.HPropertyAudits)
                            .FirstOrDefaultAsync(p => p.HPropertyId == id);

                        if (property == null)
                        {
                            return NotFound();
                        }

                        // 1. 軟刪除房源圖片
                        if (property.HPropertyImages != null && property.HPropertyImages.Any())
                        {
                            foreach (var image in property.HPropertyImages)
                            {
                                image.HIsDelete = true;
                            }
                            await _context.SaveChangesAsync();
                        }

                        // 2. 軟刪除房源特色
                        if (property.HPropertyFeatures != null && property.HPropertyFeatures.Any())
                        {
                            foreach (var feature in property.HPropertyFeatures)
                            {
                                feature.HIsDelete = true;
                            }
                            await _context.SaveChangesAsync();
                        }

                        // 3. 軟刪除房源審核記錄
                        if (property.HPropertyAudits != null && property.HPropertyAudits.Any())
                        {
                            foreach (var audit in property.HPropertyAudits)
                            {
                                audit.HIsDelete = true;
                            }
                            await _context.SaveChangesAsync();
                        }

                        // 4. 最後軟刪除房源本身
                        property.HIsDelete = true;
                        await _context.SaveChangesAsync();

                        // 提交交易
                        await transaction.CommitAsync();
                        TempData["SuccessMessage"] = "房源已成功刪除！";

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // 回滾交易
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = $"刪除房源時發生錯誤：{ex.Message}";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"刪除房源時發生錯誤：{ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
