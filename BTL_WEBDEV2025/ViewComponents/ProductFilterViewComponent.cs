using Microsoft.AspNetCore.Mvc;
using BTL_WEBDEV2025.Data;
using Microsoft.EntityFrameworkCore;

namespace BTL_WEBDEV2025.ViewComponents
{
    public class ProductFilterViewComponent : ViewComponent
    {
        private readonly AppDbContext _db;
        
        public ProductFilterViewComponent(AppDbContext db)
        {
            _db = db;
        }
        
        public IViewComponentResult Invoke(string minPriceId = "minPrice", string maxPriceId = "maxPrice", 
            string brandSelectId = "brandSelect", string sortSelectId = "sortPrice", 
            string applyButtonId = "applyFilters", string resetButtonId = "resetFilters")
        {
            List<string> brands;
            
            try
            {
                if (_db.Database.CanConnect())
                {
                    brands = _db.Brands.Select(b => b.Name).ToList();
                }
                else
                {
                    brands = new List<string> { "Nike", "Adidas", "Balenciaga", "Louboutin", "Puma", "New Balance", "ASICS", "Reebok" };
                }
            }
            catch
            {
                brands = new List<string> { "Nike", "Adidas", "Balenciaga", "Louboutin", "Puma", "New Balance", "ASICS", "Reebok" };
            }
            
            return View(new ProductFilterViewModel
            {
                Brands = brands,
                MinPriceId = minPriceId,
                MaxPriceId = maxPriceId,
                BrandSelectId = brandSelectId,
                SortSelectId = sortSelectId,
                ApplyButtonId = applyButtonId,
                ResetButtonId = resetButtonId
            });
        }
    }
    
    public class ProductFilterViewModel
    {
        public List<string> Brands { get; set; } = new();
        public string MinPriceId { get; set; } = "minPrice";
        public string MaxPriceId { get; set; } = "maxPrice";
        public string BrandSelectId { get; set; } = "brandSelect";
        public string SortSelectId { get; set; } = "sortPrice";
        public string ApplyButtonId { get; set; } = "applyFilters";
        public string ResetButtonId { get; set; } = "resetFilters";
    }
}

