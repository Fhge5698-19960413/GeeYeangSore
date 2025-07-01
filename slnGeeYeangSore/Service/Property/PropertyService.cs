using GeeYeangSore.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Property
{
    public class PropertyService : IPropertyService
    {
        private readonly GeeYeangSoreContext _context;

        public PropertyService(GeeYeangSoreContext context)
        {
            this._context = context;
        }

        public async Task<List<HProperty>> GetPropertyListByAsync(string sortOrder, string searchString, string searchType)
        {
            var properties = _context.HProperties
                    .Include(p => p.HLandlord)
                    .Include(p => p.HPropertyFeatures)
                    .Include(p => p.HPropertyImages)
                    .Where(p => p.HStatus == "已驗證" && p.HLandlord.HStatus == "已驗證" && (p.HIsDelete == null || p.HIsDelete == false))  // 只篩選已驗證的房源和已驗證的房東，且未被刪除
                    .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim();
                switch (searchType?.ToLower())
                {
                    case "id":
                        if (int.TryParse(searchString, out int propertyId))
                        {
                            properties = properties.Where(p => p.HPropertyId == propertyId);
                        }
                        break;
                    case "title":
                        properties = properties.Where(p => p.HPropertyTitle.Contains(searchString));
                        break;
                    case "address":
                        properties = properties.Where(p => p.HAddress.Contains(searchString));
                        break;
                    case "city":
                        properties = properties.Where(p => p.HCity.Contains(searchString));
                        break;
                    case "district":
                        properties = properties.Where(p => p.HDistrict.Contains(searchString));
                        break;
                    case "landlord":
                        properties = properties.Where(p => p.HLandlord.HLandlordName.Contains(searchString));
                        break;
                    case "type":
                        properties = properties.Where(p => p.HPropertyType.Contains(searchString));
                        break;
                    case "rent":
                        if (int.TryParse(searchString, out int rentPrice))
                        {
                            properties = properties.Where(p => p.HRentPrice == rentPrice);
                        }
                        break;
                    default: // "all" or any other value
                        properties = properties.Where(p =>
                            p.HPropertyId.ToString().Contains(searchString) ||
                            p.HPropertyTitle.Contains(searchString) ||
                            p.HAddress.Contains(searchString) ||
                            p.HCity.Contains(searchString) ||
                            p.HDistrict.Contains(searchString) ||
                            p.HPropertyType.Contains(searchString) ||
                            p.HRentPrice.ToString().Contains(searchString) ||
                            p.HLandlord.HLandlordName.Contains(searchString));
                        break;
                }
            }
            // 根據排序參數進行排序
            switch (sortOrder)
            {
                case "id_desc":
                    properties = properties.OrderByDescending(p => p.HPropertyId);
                    break;
                case "id":
                    properties = properties.OrderBy(p => p.HPropertyId);
                    break;
                case "title_desc":
                    properties = properties.OrderByDescending(p => p.HPropertyTitle);
                    break;
                case "title":
                    properties = properties.OrderBy(p => p.HPropertyTitle);
                    break;
                case "landlord_desc":
                    properties = properties.OrderByDescending(p => p.HLandlord.HLandlordName);
                    break;
                case "landlord":
                    properties = properties.OrderBy(p => p.HLandlord.HLandlordName);
                    break;
                case "address_desc":
                    properties = properties.OrderByDescending(p => p.HAddress);
                    break;
                case "address":
                    properties = properties.OrderBy(p => p.HAddress);
                    break;
                case "price":
                    properties = properties.OrderBy(p => p.HRentPrice);
                    break;
                case "price_desc":
                    properties = properties.OrderByDescending(p => p.HRentPrice);
                    break;
                case "date":
                    properties = properties.OrderBy(p => p.HPublishedDate);
                    break;
                case "date_desc":
                    properties = properties.OrderByDescending(p => p.HPublishedDate);
                    break;
                default: // "id" or empty
                    properties = properties.OrderBy(p => p.HPropertyId);
                    break;
            }
            return await properties.ToListAsync();
        }

        public async Task<HProperty> GetPropertyByIdAsync(int propertyId)
        {
            return await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .FirstOrDefaultAsync(p => p.HPropertyId == propertyId);
        }

        public async Task<List<HProperty>> GetPropertiesByLandlordIdAsync(int landlordId)
        {
            return await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .Where(p => p.HLandlordId == landlordId && (p.HIsDelete == null || p.HIsDelete == false))
                .OrderByDescending(p => p.HPublishedDate)
                .ToListAsync();
        }

        public async Task<List<HProperty>> GetPropertiesByStatusAsync(string status)
        {
            return await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .Where(p => p.HStatus == status && (p.HIsDelete == null || p.HIsDelete == false))
                .OrderByDescending(p => p.HPublishedDate)
                .ToListAsync();
        }

        public async Task<List<HProperty>> GetPropertiesByCityAsync(string city)
        {
            return await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .Where(p => p.HCity == city && p.HStatus == "已驗證" && (p.HIsDelete == null || p.HIsDelete == false))
                .OrderByDescending(p => p.HPublishedDate)
                .ToListAsync();
        }

        public async Task<List<HProperty>> GetPropertiesByDistrictAsync(string district)
        {
            return await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .Where(p => p.HDistrict == district && p.HStatus == "已驗證" && (p.HIsDelete == null || p.HIsDelete == false))
                .OrderByDescending(p => p.HPublishedDate)
                .ToListAsync();
        }

        public async Task<List<HProperty>> GetPropertiesByPriceRangeAsync(int minPrice, int maxPrice)
        {
            return await _context.HProperties
                .Include(p => p.HLandlord)
                .Include(p => p.HPropertyFeatures)
                .Include(p => p.HPropertyImages)
                .Where(p => p.HRentPrice >= minPrice && p.HRentPrice <= maxPrice && 
                           p.HStatus == "已驗證" && (p.HIsDelete == null || p.HIsDelete == false))
                .OrderBy(p => p.HRentPrice)
                .ToListAsync();
        }

        public async Task<bool> CreatePropertyAsync(HProperty property)
        {
            try
            {
                property.HPublishedDate = DateTime.Now;
                property.HLastUpdated = DateTime.Now;
                property.HIsDelete = false;
                _context.HProperties.Add(property);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdatePropertyAsync(HProperty property)
        {
            try
            {
                var existingProperty = await _context.HProperties.FindAsync(property.HPropertyId);
                if (existingProperty == null)
                    return false;

                existingProperty.HPropertyTitle = property.HPropertyTitle;
                existingProperty.HDescription = property.HDescription;
                existingProperty.HAddress = property.HAddress;
                existingProperty.HCity = property.HCity;
                existingProperty.HDistrict = property.HDistrict;
                existingProperty.HZipcode = property.HZipcode;
                existingProperty.HRentPrice = property.HRentPrice;
                existingProperty.HPropertyType = property.HPropertyType;
                existingProperty.HRoomCount = property.HRoomCount;
                existingProperty.HBathroomCount = property.HBathroomCount;
                existingProperty.HArea = property.HArea;
                existingProperty.HFloor = property.HFloor;
                existingProperty.HTotalFloors = property.HTotalFloors;
                existingProperty.HAvailabilityStatus = property.HAvailabilityStatus;
                existingProperty.HBuildingType = property.HBuildingType;
                existingProperty.HIsVip = property.HIsVip;
                existingProperty.HIsShared = property.HIsShared;
                existingProperty.HLatitude = property.HLatitude;
                existingProperty.HLongitude = property.HLongitude;
                existingProperty.HLastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeletePropertyAsync(int propertyId)
        {
            try
            {
                var property = await _context.HProperties.FindAsync(propertyId);
                if (property == null)
                    return false;

                property.HIsDelete = true;
                property.HLastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActivatePropertyAsync(int propertyId)
        {
            try
            {
                var property = await _context.HProperties.FindAsync(propertyId);
                if (property == null)
                    return false;

                property.HStatus = "已驗證";
                property.HLastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivatePropertyAsync(int propertyId)
        {
            try
            {
                var property = await _context.HProperties.FindAsync(propertyId);
                if (property == null)
                    return false;

                property.HStatus = "未驗證";
                property.HLastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PropertyExistsAsync(int propertyId)
        {
            return await _context.HProperties.AnyAsync(p => p.HPropertyId == propertyId);
        }
    }
}
