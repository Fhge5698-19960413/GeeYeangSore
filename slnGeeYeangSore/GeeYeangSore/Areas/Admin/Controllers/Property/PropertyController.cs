using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GeeYeangSore.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using X.PagedList;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using GeeYeangSore.Controllers;
using Service.Property;

namespace GeeYeangSore.Areas.Admin.Controllers.Property
{
    /// <summary>
    /// 房源管理控制器
    /// </summary>
    /// // GET:https://localhost:7022/Admin/Property/Index
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class PropertyController : SuperController
    {
        private readonly IPropertyService _propertyService;
        private readonly GeeYeangSoreContext _context;
        private readonly ILogger<PropertyController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropertyController(IPropertyService propertyService, GeeYeangSoreContext context, ILogger<PropertyController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _propertyService = propertyService;
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }
        
        /// <summary>
        /// 房源列表頁面，支援搜尋功能
        /// </summary>
        /// <param name="searchString">搜尋關鍵字</param>
        /// <param name="searchType">搜尋類型</param>
        /// <param name="sortOrder">排序參數</param>
        /// <param name="page">當前頁碼</param>
        /// <returns>房源列表視圖</returns>
        public async Task<IActionResult> Index(string searchString, string searchType, string sortOrder, int page = 1)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            try
            {
                int pageSize = 15;

                // 設置排序參數
                ViewData["IdSort"] = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
                ViewData["TitleSort"] = sortOrder == "title" ? "title_desc" : "title";
                ViewData["LandlordSort"] = sortOrder == "landlord" ? "landlord_desc" : "landlord";
                ViewData["AddressSort"] = sortOrder == "address" ? "address_desc" : "address";
                ViewData["PriceSort"] = sortOrder == "price" ? "price_desc" : "price";
                ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
                ViewData["CurrentSort"] = sortOrder;
                ViewData["CurrentFilter"] = searchString;
                ViewData["SearchType"] = searchType;

                // 使用服務層取得資料
                var result = await _propertyService.GetPropertyListByAsync(sortOrder, searchString, searchType);
                return View(result.ToPagedList(page, pageSize));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving properties");
                TempData["ErrorMessage"] = "取得房源列表時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 顯示房源詳細資訊
        /// </summary>
        /// <param name="id">房源ID</param>
        /// <returns>房源詳細資訊視圖</returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var property = await _propertyService.GetPropertyByIdAsync(id.Value);
                if (property == null)
                {
                    return NotFound();
                }

                return View(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving property details");
                TempData["ErrorMessage"] = "取得房源詳細資訊時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 顯示新增房源頁面
        /// </summary>
        public IActionResult Create()
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            try
            {
                SetViewData(null);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while preparing create view");
                TempData["ErrorMessage"] = "準備新增頁面時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 處理新增房源的請求
        /// </summary>
        /// <param name="property">房源資料</param>
        /// <param name="features">房源特色</param>
        /// <param name="images">房源照片</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HProperty property, HPropertyFeature features, List<IFormFile> images)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });

            try
            {
                // 設置預設值
                property.HStatus = "已驗證";
                property.HScore = string.IsNullOrEmpty(property.HScore) ? "0" : property.HScore;
                property.HIsVip = property.HIsVip ?? false;
                property.HIsShared = property.HIsShared ?? false;
                property.HAvailabilityStatus = string.IsNullOrEmpty(property.HAvailabilityStatus) ? "可租" : property.HAvailabilityStatus;

                // 驗證房東是否存在且已驗證
                var landlord = await _context.HLandlords
                    .FirstOrDefaultAsync(l => l.HLandlordId == property.HLandlordId);
                
                if (landlord == null)
                {
                    ModelState.AddModelError("HLandlordId", "選擇的房東不存在");
                    SetViewData(property);
                    return View(property);
                }

                // 開始交易
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 使用服務層建立房源
                        var createResult = await _propertyService.CreatePropertyAsync(property);
                        if (!createResult)
                        {
                            throw new Exception("無法建立房源");
                        }

                        // 設置房源特色的關聯
                        features.HPropertyId = property.HPropertyId;
                        features.HLandlordId = property.HLandlordId;

                        // 處理 checkbox 值
                        ProcessFeatureCheckboxes(features);

                        // 保存房源特色
                        _context.Add(features);
                        await _context.SaveChangesAsync();

                        // 創建審核記錄
                        var propertyAudit = new HPropertyAudit
                        {
                            HPropertyId = property.HPropertyId,
                            HLandlordId = property.HLandlordId,
                            HAuditStatus = "已驗證",
                            HAuditDate = DateTime.Now,
                            HAuditNotes = "已驗證"
                        };
                        _context.HPropertyAudits.Add(propertyAudit);
                        await _context.SaveChangesAsync();

                        // 處理照片上傳
                        await ProcessImageUploads(images, property.HPropertyId, property.HLandlordId);

                        // 提交交易
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "房源新增成功！";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // 回滾交易
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error occurred while creating property: {0}", ex.Message);
                        ModelState.AddModelError("", "創建房源時發生錯誤: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating property: {0}", ex.Message);
                ModelState.AddModelError("", "創建房源時發生錯誤: " + ex.Message);
            }

            // 如果新增失敗，重新載入房東列表並返回視圖
            SetViewData(property);
            return View(property);
        }

        /// <summary>
        /// 顯示編輯房源頁面
        /// </summary>
        /// <param name="id">房源ID</param>
        public async Task<IActionResult> Edit(int? id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });
            
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var property = await _propertyService.GetPropertyByIdAsync(id.Value);
                if (property == null)
                {
                    return NotFound();
                }

                SetViewData(property);
                return View(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while preparing edit view");
                TempData["ErrorMessage"] = "準備編輯頁面時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 處理編輯房源的請求
        /// </summary>
        /// <param name="id">房源ID</param>
        /// <param name="property">更新後的房源資料</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, HProperty property, List<IFormFile> images, List<int> deletedImageIds)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });

            if (id != property.HPropertyId)
            {
                return NotFound();
            }

            try
            {
                // 獲取原始房源資料以保留未修改的欄位
                var existingProperty = await _propertyService.GetPropertyByIdAsync(id);
                if (existingProperty == null)
                {
                    return NotFound();
                }

                // 保留原有的狀態和發布日期
                property.HStatus = existingProperty.HStatus;
                property.HPublishedDate = existingProperty.HPublishedDate;

                // 開始交易
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // 使用服務層更新房源
                        var updateResult = await _propertyService.UpdatePropertyAsync(property);
                        if (!updateResult)
                        {
                            throw new Exception("無法更新房源");
                        }
                        
                        // 處理要刪除的照片
                        await ProcessImageDeletions(deletedImageIds);

                        // 更新房源特色
                        var features = existingProperty.HPropertyFeatures.FirstOrDefault();
                        if (features != null)
                        {
                            ProcessFeatureCheckboxes(features);
                            _context.Update(features);
                            await _context.SaveChangesAsync();
                        }

                        // 處理新上傳的照片
                        await ProcessImageUploads(images, property.HPropertyId, property.HLandlordId);

                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "房源更新成功！";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error occurred while updating property: {0}", ex.Message);
                        ModelState.AddModelError("", "更新房源時發生錯誤: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating property: {0}", ex.Message);
                ModelState.AddModelError("", "更新房源時發生錯誤: " + ex.Message);
            }

            try
            {
                // 如果更新失敗，重新載入資料
                var refreshedProperty = await _propertyService.GetPropertyByIdAsync(id);
                SetViewData(refreshedProperty ?? property);
                return View(refreshedProperty ?? property);
            }
            catch
            {
                // 最後的後備方案
                SetViewData(property);
                return View(property);
            }
        }

        /// <summary>
        /// 顯示刪除確認頁面
        /// </summary>
        /// <param name="id">房源ID</param>
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasAnyRole("超級管理員", "系統管理員", "內容管理員"))
                return RedirectToAction("NoPermission", "Home", new { area = "Admin" });

            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var property = await _propertyService.GetPropertyByIdAsync(id.Value);
                if (property == null)
                {
                    return NotFound();
                }

                return View(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while preparing delete view");
                TempData["ErrorMessage"] = "準備刪除頁面時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// 處理刪除房源的請求
        /// </summary>
        /// <param name="id">房源ID</param>
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
                            .Include(p => p.HPropertyAudits)
                            .Include(p => p.HPropertyFeatures)
                            .Include(p => p.HPropertyImages)
                            .FirstOrDefaultAsync(p => p.HPropertyId == id);

                        if (property == null)
                        {
                            return NotFound();
                        }

                        // 1. 軟刪除房源審核記錄
                        if (property.HPropertyAudits != null)
                        {
                            foreach (var audit in property.HPropertyAudits)
                            {
                                audit.HIsDelete = true;
                            }
                            await _context.SaveChangesAsync();
                        }

                        // 2. 軟刪除房源特色
                        if (property.HPropertyFeatures != null)
                        {
                            foreach (var feature in property.HPropertyFeatures)
                            {
                                feature.HIsDelete = true;
                            }
                            await _context.SaveChangesAsync();
                        }

                        // 3. 軟刪除房源圖片
                        if (property.HPropertyImages != null)
                        {
                            foreach (var image in property.HPropertyImages)
                            {
                                image.HIsDelete = true;
                            }
                            await _context.SaveChangesAsync();
                        }

                        // 4. 使用服務層軟刪除房源本身
                        var deleteResult = await _propertyService.DeletePropertyAsync(id);
                        if (!deleteResult)
                        {
                            throw new Exception("無法刪除房源");
                        }

                        // 提交交易
                        await transaction.CommitAsync();
                        TempData["SuccessMessage"] = "房源已成功刪除！";

                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // 回滾交易
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error occurred while deleting property and its related data");
                        TempData["ErrorMessage"] = "刪除房源時發生錯誤";
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting property");
                TempData["ErrorMessage"] = "刪除房源時發生錯誤";
                return RedirectToAction("Index");
            }
        }

        #region Private Helper Methods

        private void SetViewData(HProperty property)
        {
            ViewData["HLandlordId"] = new SelectList(_context.HLandlords.Where(l => l.HStatus == "已驗證"), "HLandlordId", "HLandlordName", property?.HLandlordId);
        }

        private void ProcessFeatureCheckboxes(HPropertyFeature features)
        {
            features.HAllowsAnimals = Request.Form["features.HAllowsAnimals"].ToString() == "on";
            features.HAllowsDogs = Request.Form["features.HAllowsDogs"].ToString() == "on";
            features.HAllowsCats = Request.Form["features.HAllowsCats"].ToString() == "on";
            features.HAllowsCooking = Request.Form["features.HAllowsCooking"].ToString() == "on";
            features.HHasFurniture = Request.Form["features.HHasFurniture"].ToString() == "on";
            features.HInternet = Request.Form["features.HInternet"].ToString() == "on";
            features.HAirConditioning = Request.Form["features.HAirConditioning"].ToString() == "on";
            features.HSharedRental = Request.Form["features.HSharedRental"].ToString() == "on";
            features.HTv = Request.Form["features.HTv"].ToString() == "on";
            features.HRefrigerator = Request.Form["features.HRefrigerator"].ToString() == "on";
            features.HWashingMachine = Request.Form["features.HWashingMachine"].ToString() == "on";
            features.HBed = Request.Form["features.HBed"].ToString() == "on";
            features.HWaterHeater = Request.Form["features.HWaterHeater"].ToString() == "on";
            features.HGasStove = Request.Form["features.HGasStove"].ToString() == "on";
            features.HCableTv = Request.Form["features.HCableTv"].ToString() == "on";
            features.HWaterDispenser = Request.Form["features.HWaterDispenser"].ToString() == "on";
            features.HParking = Request.Form["features.HParking"].ToString() == "on";
            features.HSocialHousing = Request.Form["features.HSocialHousing"].ToString() == "on";
            features.HShortTermRent = Request.Form["features.HShortTermRent"].ToString() == "on";
            features.HPublicElectricity = Request.Form["features.HPublicElectricity"].ToString() == "on";
            features.HPublicWatercharges = Request.Form["features.HPublicWatercharges"].ToString() == "on";
            features.HLandlordShared = Request.Form["features.HLandlordShared"].ToString() == "on";
            features.HBalcony = Request.Form["features.HBalcony"].ToString() == "on";
            features.HPublicEquipment = Request.Form["features.HPublicEquipment"].ToString() == "on";
        }

        private async Task ProcessImageUploads(List<IFormFile> images, int propertyId, int landlordId)
        {
            if (images != null && images.Count > 0 && images[0].Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "Property");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var image in images)
                {
                    if (image.Length > 0)
                    {
                        try
                        {
                            // 生成唯一的檔案名
                            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            // 保存檔案
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }

                            // 創建圖片記錄
                            var propertyImage = new HPropertyImage
                            {
                                HPropertyId = propertyId,
                                HLandlordId = landlordId,
                                HImageUrl = "/images/Property/" + uniqueFileName,
                                HUploadedDate = DateTime.Now,
                                HLastUpDated = DateTime.Now
                            };

                            _context.HPropertyImages.Add(propertyImage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error saving image: {0}", ex.Message);
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task ProcessImageDeletions(List<int> deletedImageIds)
        {
            if (deletedImageIds != null && deletedImageIds.Any())
            {
                var imagesToDelete = await _context.HPropertyImages
                    .Where(img => deletedImageIds.Contains(img.HImageId))
                    .ToListAsync();

                foreach (var image in imagesToDelete)
                {
                    if (!string.IsNullOrEmpty(image.HImageUrl))
                    {
                        string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, image.HImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }  
                }
                _context.HPropertyImages.RemoveRange(imagesToDelete);
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}


