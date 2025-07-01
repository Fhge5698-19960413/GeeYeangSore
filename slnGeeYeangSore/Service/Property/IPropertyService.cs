using GeeYeangSore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Property
{
    public interface IPropertyService
    {
        Task<List<HProperty>> GetPropertyListByAsync(string sortOrder, string searchString, string searchType);
        Task<HProperty> GetPropertyByIdAsync(int propertyId);
        Task<List<HProperty>> GetPropertiesByLandlordIdAsync(int landlordId);
        Task<List<HProperty>> GetPropertiesByStatusAsync(string status);
        Task<List<HProperty>> GetPropertiesByCityAsync(string city);
        Task<List<HProperty>> GetPropertiesByDistrictAsync(string district);
        Task<List<HProperty>> GetPropertiesByPriceRangeAsync(int minPrice, int maxPrice);
        Task<bool> CreatePropertyAsync(HProperty property);
        Task<bool> UpdatePropertyAsync(HProperty property);
        Task<bool> DeletePropertyAsync(int propertyId);
        Task<bool> ActivatePropertyAsync(int propertyId);
        Task<bool> DeactivatePropertyAsync(int propertyId);
        Task<bool> PropertyExistsAsync(int propertyId);
    }
}
