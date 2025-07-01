using GeeYeangSore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Property
{
    public interface IPropertyADService
    {
        Task<List<HAd>> GetAdListByAsync(string sortOrder, string searchString, string searchType);
        Task<HAd> GetAdByIdAsync(int adId);
        Task<bool> CreateAdAsync(HAd ad);
        Task<bool> UpdateAdAsync(HAd ad);
        Task<bool> DeleteAdAsync(int adId);
    }
}
