using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class VendorService : IVendorService
    {
        private readonly AppDbContext _context;

        public VendorService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<VendorResponseDTO>> GetAllVendorsAsync()
        {
            return await _context.Vendors
                .Include(v => v.Purchases)
                .OrderBy(v => v.Name)
                .Select(v => new VendorResponseDTO
                {
                    VendorId = v.VendorId,
                    Name = v.Name,
                    ContactPerson = v.ContactPerson,
                    Phone = v.Phone,
                    Email = v.Email,
                    Address = v.Address,
                    CreatedAt = v.CreatedAt,
                    TotalPurchases = v.Purchases.Count
                })
                .ToListAsync();
        }

        public async Task<VendorResponseDTO?> GetVendorByIdAsync(int vendorId)
        {
            var vendor = await _context.Vendors
                .Include(v => v.Purchases)
                    .ThenInclude(p => p.PurchaseItems)
                        .ThenInclude(pi => pi.Part)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

            if (vendor == null) return null;

            return new VendorResponseDTO
            {
                VendorId = vendor.VendorId,
                Name = vendor.Name,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                CreatedAt = vendor.CreatedAt,
                TotalPurchases = vendor.Purchases?.Count ?? 0,
                PurchaseHistory = vendor.Purchases?.Select(p => new PurchaseResponseDTO
                {
                    PurchaseId = p.PurchaseId,
                    VendorId = p.VendorId,
                    VendorName = vendor.Name,
                    TotalAmount = p.TotalAmount,
                    CreatedAt = p.CreatedAt,
                    Items = p.PurchaseItems?.Select(pi => new PurchaseItemResponseDTO
                    {
                        PurchaseItemId = pi.PurchaseItemId,
                        PartId = pi.PartId,
                        PartName = pi.Part?.Name ?? "Unknown Part",
                        Quantity = pi.Quantity,
                        UnitCost = pi.UnitCost,
                        Subtotal = pi.Subtotal
                    }).ToList() ?? new()
                }).OrderByDescending(p => p.CreatedAt).ToList() ?? new()
            };
        }

        public async Task<VendorResponseDTO> CreateVendorAsync(CreateVendorDTO dto)
        {
            bool phoneExists = await _context.Vendors.AnyAsync(v => v.Phone == dto.Phone);
            if (phoneExists)
                throw new InvalidOperationException("A vendor with this phone number already exists.");

            var vendor = new Vendor
            {
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return new VendorResponseDTO
            {
                VendorId = vendor.VendorId,
                Name = vendor.Name,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                CreatedAt = vendor.CreatedAt,
                TotalPurchases = 0
            };
        }

        public async Task<VendorResponseDTO?> UpdateVendorAsync(int vendorId, UpdateVendorDTO dto)
        {
            var vendor = await _context.Vendors
                .Include(v => v.Purchases)
                .FirstOrDefaultAsync(v => v.VendorId == vendorId);

            if (vendor == null) return null;

            bool phoneConflict = await _context.Vendors
                .AnyAsync(v => v.Phone == dto.Phone && v.VendorId != vendorId);
            if (phoneConflict)
                throw new InvalidOperationException("Another vendor with this phone number already exists.");

            vendor.Name = dto.Name;
            vendor.ContactPerson = dto.ContactPerson;
            vendor.Phone = dto.Phone;
            vendor.Email = dto.Email;
            vendor.Address = dto.Address;

            await _context.SaveChangesAsync();

            return new VendorResponseDTO
            {
                VendorId = vendor.VendorId,
                Name = vendor.Name,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                CreatedAt = vendor.CreatedAt,
                TotalPurchases = vendor.Purchases?.Count ?? 0
            };
        }

        public async Task<bool> DeleteVendorAsync(int vendorId)
        {
            var vendor = await _context.Vendors.FindAsync(vendorId);
            if (vendor == null) return false;

            bool hasPurchases = await _context.Purchases.AnyAsync(p => p.VendorId == vendorId);
            if (hasPurchases)
                throw new InvalidOperationException("Cannot delete a vendor that has existing purchase records.");

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}