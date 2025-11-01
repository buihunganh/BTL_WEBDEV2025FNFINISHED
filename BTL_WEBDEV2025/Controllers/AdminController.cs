using BTL_WEBDEV2025.Models;
using BTL_WEBDEV2025.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BTL_WEBDEV2025.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminController(ILogger<AdminController> logger, AppDbContext db, IWebHostEnvironment env)
        {
            _logger = logger;
            _db = db;
            _env = env;
        }

        // GET: Admin
        public IActionResult Index()
        {
            // Simple authentication check (có thể mở rộng với ASP.NET Identity)
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }
            
            // View hiện render theo tab qua query string; dữ liệu sẽ nạp bằng AJAX từ API bên dưới
            return View();
        }

        // GET: Admin/Login => dùng luồng đăng nhập hợp nhất
        public IActionResult Login()
        {
            return RedirectToAction("Login", "Account");
        }

        // =====================
        // JSON API for Dashboard
        // =====================
        [HttpGet("/admin/api/stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!IsAdmin()) return Unauthorized();

            // Doanh số (USD) = tổng TotalAmount trong Orders
            var totalSales = await _db.Orders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var totalOrders = await _db.Orders.CountAsync();
            // Khách hàng = Users trừ admin (RoleId != 1)
            var totalCustomers = await _db.Users.CountAsync(u => u.RoleId != 1);

            return Ok(new
            {
                totalSales,
                totalOrders,
                totalCustomers
            });
        }

        // =====================
        // REPORT (minimal): summary & timeseries by day
        // =====================
        [HttpGet("/admin/api/report/summary")]
        public async Task<IActionResult> ReportSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!IsAdmin()) return Unauthorized();
            var start = (from ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            var end = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1); // inclusive end of day

            var ordersInRange = _db.Orders.Where(o => o.CreatedAt >= start && o.CreatedAt <= end);
            var revenue = await ordersInRange.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var orders = await ordersInRange.CountAsync();
            var items = await _db.OrderDetails
                .Where(od => ordersInRange.Select(o => o.Id).Contains(od.OrderId))
                .SumAsync(od => (int?)od.Quantity) ?? 0;
            var aov = orders == 0 ? 0 : revenue / orders;

            return Ok(new { revenueUSD = revenue, orders, items, aov });
        }

        [HttpGet("/admin/api/report/timeseries")]
        public async Task<IActionResult> ReportTimeseries([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            if (!IsAdmin()) return Unauthorized();
            var start = (from ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            var end = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);

            var query = await _db.Orders
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { period = g.Key, revenueUSD = g.Sum(x => x.TotalAmount), orders = g.Count() })
                .OrderBy(x => x.period)
                .ToListAsync();

            var result = query.Select(x => new { period = x.period.ToString("yyyy-MM-dd"), x.revenueUSD, x.orders });
            return Ok(result);
        }

        // =====================
        // PRODUCTS CRUD (JSON)
        // =====================
        [HttpGet("/admin/api/products")]
        public async Task<IActionResult> GetProducts()
        {
            if (!IsAdmin()) return Unauthorized();
            var list = await _db.Products
                .Include(p => p.CategoryRef)
                .Include(p => p.Brand)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    Category = p.CategoryRef != null ? p.CategoryRef.Name : (string.IsNullOrEmpty(p.Category) ? "" : p.Category),
                    p.ImageUrl
                }).ToListAsync();
            return Ok(list);
        }

        public class ProductUpsertDto
        {
            public int? Id { get; set; }
            [Required]
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            [Range(0, double.MaxValue)]
            public decimal Price { get; set; }
            public string? ImageUrl { get; set; }
            public string? Category { get; set; }
        }

        public class InventoryUpdateDto
        {
            public int StockQuantity { get; set; }
        }

        [HttpPost("/admin/api/products/create")]
        public async Task<IActionResult> CreateProduct([FromBody] ProductUpsertDto dto)
        {
            if (!IsAdmin()) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                Price = dto.Price,
                ImageUrl = dto.ImageUrl ?? string.Empty,
                Category = dto.Category ?? string.Empty
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return Ok(new { product.Id });
        }

        [HttpPost("/admin/api/products/update/{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] ProductUpsertDto dto)
        {
            if (!IsAdmin()) return Unauthorized();
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            product.Name = dto.Name ?? product.Name;
            product.Description = dto.Description ?? product.Description;
            product.Price = dto.Price;
            product.ImageUrl = dto.ImageUrl ?? product.ImageUrl;
            if (!string.IsNullOrWhiteSpace(dto.Category)) product.Category = dto.Category!;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("/admin/api/products/delete/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // =====================
        // INVENTORY (ProductVariants)
        // =====================
        [HttpGet("/admin/api/inventory")]
        public IActionResult GetInventory()
        {
            if (!IsAdmin()) return Unauthorized();
            var rows = _db.ProductVariants
                .Select(v => new
                {
                    v.Id,
                    v.ProductId,
                    ProductName = _db.Products.Where(p => p.Id == v.ProductId).Select(p => p.Name).FirstOrDefault() ?? "",
                    v.Size,
                    v.Color,
                    v.StockQuantity
                }).ToList();
            return Ok(rows);
        }

        [HttpPost("/admin/api/inventory/update/{id:int}")]
        public IActionResult UpdateInventory(int id, [FromBody] InventoryUpdateDto dto)
        {
            if (!IsAdmin()) return Unauthorized();
            var variant = _db.ProductVariants.FirstOrDefault(v => v.Id == id);
            if (variant == null) return NotFound();
            variant.StockQuantity = Math.Max(0, dto.StockQuantity);
            _db.SaveChanges();
            return Ok();
        }

        // =====================
        // CUSTOMERS CRUD (Users)
        // =====================
        [HttpGet("/admin/api/customers")]
        public async Task<IActionResult> GetCustomers()
        {
            if (!IsAdmin()) return Unauthorized();
            var list = await _db.Users
                .Where(u => u.RoleId != 1)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.DateOfBirth
                }).ToListAsync();
            return Ok(list);
        }

        public class CustomerUpsertDto
        {
            public int? Id { get; set; }
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required]
            public string FullName { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public DateTime? DateOfBirth { get; set; }
        }

        [HttpPost("/admin/api/customers/create")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerUpsertDto dto)
        {
            if (!IsAdmin()) return Unauthorized();
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var user = new User
            {
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                DateOfBirth = dto.DateOfBirth,
                PasswordHash = "temp", // nên thay bằng quy trình tạo tài khoản chuẩn
                RoleId = 2
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok(new { user.Id });
        }

        [HttpPost("/admin/api/customers/update/{id:int}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerUpsertDto dto)
        {
            if (!IsAdmin()) return Unauthorized();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId != 1);
            if (user == null) return NotFound();
            user.Email = dto.Email;
            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.DateOfBirth = dto.DateOfBirth;
            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("/admin/api/customers/delete/{id:int}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.RoleId != 1);
            if (user == null) return NotFound();
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok();
        }

        // POST: Admin/Login => chuyển sang luồng Account/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            return RedirectToAction("Login", "Account");
        }

        // GET: Admin/Logout (link trong menu)
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsAdmin");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserEmail");
            return RedirectToAction("Login", "Account");
        }

        // GET: Admin/Create
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }
            
            return View();
        }

        // POST: Admin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create([Bind("Name,Description,Price,DiscountPrice,ImageUrl,Category")] Product product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            if (ModelState.IsValid)
            {
                // Save to database
                _db.Products.Add(product);
                _db.SaveChanges();
                
                TempData["Success"] = "Product created successfully";
                return RedirectToAction("Index");
            }
            
            return View(product);
        }

        // GET: Admin/Edit/5
        public IActionResult Edit(int? id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            if (id == null)
            {
                return NotFound();
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            
            return View(product);
        }

        // POST: Admin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("Id,Name,Description,Price,DiscountPrice,ImageUrl,Category")] Product product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var existingProduct = _db.Products.FirstOrDefault(p => p.Id == id);
                if (existingProduct != null)
                {
                    existingProduct.Name = product.Name;
                    existingProduct.Description = product.Description;
                    existingProduct.Price = product.Price;
                    existingProduct.DiscountPrice = product.DiscountPrice;
                    existingProduct.ImageUrl = product.ImageUrl;
                    existingProduct.CategoryId = product.CategoryId;
                    _db.SaveChanges();
                }
                
                TempData["Success"] = "Product updated successfully";
                return RedirectToAction("Index");
            }
            
            return View(product);
        }

        // POST: Admin/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login");
            }

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _db.Products.Remove(product);
                _db.SaveChanges();
                TempData["Success"] = "Product deleted successfully";
            }
            
            return RedirectToAction("Index");
        }

        // GET: Admin/BulkImages
        public IActionResult BulkImages()
        {
            if (!IsAdmin()) return RedirectToAction("Login");
            return View();
        }

        // POST: Admin/BulkImages
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImages(List<IFormFile> files, string? brand, string? category, bool updateOnly = false)
        {
            if (!IsAdmin()) return RedirectToAction("Login");
            if (files == null || files.Count == 0)
            {
                TempData["Success"] = "No files selected.";
                return RedirectToAction("BulkImages");
            }

            var webRoot = _env.WebRootPath;
            var safeSegment = !string.IsNullOrWhiteSpace(brand) ? brand.Trim() : (!string.IsNullOrWhiteSpace(category) ? category.Trim() : "uploads");
            foreach (var c in Path.GetInvalidFileNameChars()) safeSegment = safeSegment.Replace(c, '-');
            var destDir = Path.Combine(webRoot, "media", "products", safeSegment);
            Directory.CreateDirectory(destDir);

            int saved = 0, updated = 0, created = 0;
            foreach (var file in files)
            {
                if (file.Length <= 0) continue;
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp") continue;

                var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                // normalize filename
                var normalized = new string(baseName.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-').ToArray());
                normalized = System.Text.RegularExpressions.Regex.Replace(normalized, "-+", "-").Trim('-');
                var fileName = normalized + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ext;
                var savePath = Path.Combine(destDir, fileName);
                using (var stream = System.IO.File.Create(savePath))
                {
                    await file.CopyToAsync(stream);
                }
                saved++;

                var relUrl = "/media/products/" + safeSegment + "/" + fileName;

                // Try map to product by trailing id pattern "...-123"
                int? parsedId = null;
                var m = System.Text.RegularExpressions.Regex.Match(normalized, @"-(\d+)$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var idVal)) parsedId = idVal;

                Product? product = null;
                if (parsedId.HasValue)
                {
                    product = _db.Products.FirstOrDefault(p => p.Id == parsedId.Value);
                }
                if (product == null)
                {
                    var nameCandidate = baseName;
                    product = _db.Products.FirstOrDefault(p => p.Name == nameCandidate);
                }

                if (product != null)
                {
                    product.ImageUrl = relUrl;
                    _db.SaveChanges();
                    updated++;
                }
                else if (!updateOnly)
                {
                    var newProduct = new Product
                    {
                        Name = baseName,
                        Description = "",
                        Price = 0,
                        DiscountPrice = null,
                        ImageUrl = relUrl,
                        Category = string.IsNullOrWhiteSpace(category) ? "Unisex" : category!,
                        IsFeatured = false,
                        IsSpecialDeal = false
                    };
                    _db.Products.Add(newProduct);
                    _db.SaveChanges();
                    created++;
                }
            }

            TempData["Success"] = $"Uploaded {saved} files. Updated {updated} products, created {created}.";
            return RedirectToAction("Index");
        }

        private bool IsAdmin()
        {
            // Check UserId and RoleId from Account login
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = _db.Users.FirstOrDefault(u => u.Id == userId.Value);
                if (user != null && user.RoleId == 1) // RoleId 1 = Admin
                {
                    return true;
                }
            }
            
            return false;
        }

        // GET: Admin/Settings
        public IActionResult Settings()
        {
            if (!IsAdmin()) return RedirectToAction("Login");
            return View();
        }

        // POST: Admin/ChangeAdminPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeAdminPassword(string newPassword, string confirmPassword)
        {
            if (!IsAdmin()) return RedirectToAction("Login");

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["ChangePwdError"] = "Please fill in all required fields.";
                return RedirectToAction("Settings");
            }
            if (newPassword != confirmPassword)
            {
                TempData["ChangePwdError"] = "Password confirmation does not match.";
                return RedirectToAction("Settings");
            }
            if (newPassword.Length < 6 || newPassword.Length > 50)
            {
                TempData["ChangePwdError"] = "New password must be 6 to 50 characters.";
                return RedirectToAction("Settings");
            }

            // Nếu admin đăng nhập theo tài khoản trong bảng Users (RoleId=1), cập nhật mật khẩu của tài khoản đó
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = _db.Users.FirstOrDefault(u => u.Id == userId.Value && u.RoleId == 1);
                if (user != null)
                {
                    user.PasswordHash = newPassword; // plain per current project setup
                    _db.SaveChanges();
                    TempData["ChangePwdSuccess"] = "Admin password updated successfully.";
                    return RedirectToAction("Settings");
                }
            }

            // Fallback when session has no UserId (should not occur after auth unification)
            TempData["ChangePwdError"] = "Please sign in with an Admin account (RoleId = 1) to change password.";
            return RedirectToAction("Settings");
        }
    }
}

