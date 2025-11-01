using Microsoft.AspNetCore.Mvc;
using BTL_WEBDEV2025.Data;
using System.Text.Json;

namespace BTL_WEBDEV2025.ViewComponents
{
    public class CartBadgeViewComponent : ViewComponent
    {
        private const string CartSessionKey = "ShoppingCart";
        
        public IViewComponentResult Invoke()
        {
            var cartCount = 0;
            
            // Get cart count from session if user is authenticated
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                var cartJson = HttpContext.Session.GetString(CartSessionKey);
                if (!string.IsNullOrEmpty(cartJson))
                {
                    try
                    {
                        var cartItems = JsonSerializer.Deserialize<List<BTL_WEBDEV2025.Models.ShoppingCartItem>>(cartJson) 
                            ?? new List<BTL_WEBDEV2025.Models.ShoppingCartItem>();
                        cartCount = cartItems.Sum(x => x.Quantity);
                    }
                    catch
                    {
                        cartCount = 0;
                    }
                }
            }
            
            return View(cartCount);
        }
    }
}

