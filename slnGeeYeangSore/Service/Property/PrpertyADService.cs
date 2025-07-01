using GeeYeangSore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Property
{
    public class PropertyADService : IPropertyADService
    {
        private readonly GeeYeangSoreContext _context;

        public PropertyADService(GeeYeangSoreContext context)
        {
            this._context = context;
        }

        public async Task<List<HAd>> GetAdListByAsync(string sortOrder, string searchString, string searchType)
        {
            var ads = _context.HAds
                    .Include(a => a.HLandlord)
                    .Include(a => a.HProperty)
                    .Include(a => a.HPlan)
                    .Where(a => (a.HIsDelete == null || a.HIsDelete == false))  // 只篩選未被刪除的廣告
                    .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                switch (searchType?.ToLower())
                {
                    case "id":
                        if (int.TryParse(searchString, out int adId))
                        {
                            ads = ads.Where(a => a.HAdId == adId);
                        }
                        break;
                    case "name":
                        ads = ads.Where(a => a.HAdName.Contains(searchString));
                        break;
                    case "category":
                        ads = ads.Where(a => a.HCategory.Contains(searchString));
                        break;
                    case "landlord":
                        ads = ads.Where(a => a.HLandlord.HLandlordName.Contains(searchString));
                        break;
                    case "property":
                        ads = ads.Where(a => a.HProperty.HPropertyTitle.Contains(searchString));
                        break;
                    case "status":
                        ads = ads.Where(a => a.HStatus.Contains(searchString));
                        break;
                    case "price":
                        if (decimal.TryParse(searchString, out decimal adPrice))
                        {
                            ads = ads.Where(a => a.HAdPrice == adPrice);
                        }
                        break;
                    default: // "all" or any other value
                        ads = ads.Where(a =>
                            a.HAdId.ToString().Contains(searchString) ||
                            a.HAdName.Contains(searchString) ||
                            a.HCategory.Contains(searchString) ||
                            a.HStatus.Contains(searchString) ||
                            a.HAdPrice.ToString().Contains(searchString) ||
                            a.HLandlord.HLandlordName.Contains(searchString) ||
                            a.HProperty.HPropertyTitle.Contains(searchString));
                        break;
                }
            }

            // 根據排序參數進行排序
            switch (sortOrder)
            {
                case "id_desc":
                    ads = ads.OrderByDescending(a => a.HAdId);
                    break;
                case "id":
                    ads = ads.OrderBy(a => a.HAdId);
                    break;
                case "name_desc":
                    ads = ads.OrderByDescending(a => a.HAdName);
                    break;
                case "name":
                    ads = ads.OrderBy(a => a.HAdName);
                    break;
                case "landlord_desc":
                    ads = ads.OrderByDescending(a => a.HLandlord.HLandlordName);
                    break;
                case "landlord":
                    ads = ads.OrderBy(a => a.HLandlord.HLandlordName);
                    break;
                case "category_desc":
                    ads = ads.OrderByDescending(a => a.HCategory);
                    break;
                case "category":
                    ads = ads.OrderBy(a => a.HCategory);
                    break;
                case "price":
                    ads = ads.OrderBy(a => a.HAdPrice);
                    break;
                case "price_desc":
                    ads = ads.OrderByDescending(a => a.HAdPrice);
                    break;
                case "startdate":
                    ads = ads.OrderBy(a => a.HStartDate);
                    break;
                case "startdate_desc":
                    ads = ads.OrderByDescending(a => a.HStartDate);
                    break;
                case "enddate":
                    ads = ads.OrderBy(a => a.HEndDate);
                    break;
                case "enddate_desc":
                    ads = ads.OrderByDescending(a => a.HEndDate);
                    break;
                default: // "id" or empty
                    ads = ads.OrderBy(a => a.HAdId);
                    break;
            }
            return await ads.ToListAsync();
        }

        public async Task<HAd> GetAdByIdAsync(int adId)
        {
            return await _context.HAds
                .Include(a => a.HLandlord)
                .Include(a => a.HProperty)
                .Include(a => a.HPlan)
                .FirstOrDefaultAsync(a => a.HAdId == adId);
        }

        public async Task<bool> CreateAdAsync(HAd ad)
        {
            try
            {
                ad.HCreatedDate = DateTime.Now;
                ad.HLastUpdated = DateTime.Now;
                _context.HAds.Add(ad);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAdAsync(HAd ad)
        {
            try
            {
                var existingAd = await _context.HAds.FindAsync(ad.HAdId);
                if (existingAd == null)
                    return false;

                existingAd.HAdName = ad.HAdName;
                existingAd.HDescription = ad.HDescription;
                existingAd.HCategory = ad.HCategory;
                existingAd.HPriority = ad.HPriority;
                existingAd.HAdTag = ad.HAdTag;
                existingAd.HTargetRegion = ad.HTargetRegion;
                existingAd.HStatus = ad.HStatus;
                existingAd.HAdPrice = ad.HAdPrice;
                existingAd.HLinkUrl = ad.HLinkUrl;
                existingAd.HImageUrl = ad.HImageUrl;
                existingAd.HStartDate = ad.HStartDate;
                existingAd.HEndDate = ad.HEndDate;
                existingAd.HLastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAdAsync(int adId)
        {
            try
            {
                var ad = await _context.HAds.FindAsync(adId);
                if (ad == null)
                    return false;

                ad.HIsDelete = true;
                ad.HLastUpdated = DateTime.Now;
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
