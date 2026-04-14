namespace CustomerPortal.Services
{
    public class CartState
    {
        public List<CartItem> Items { get; } = [];

        public event Action? OnChange;

        public void AddItem(ProductDto product)
        {
            var existing = Items.FirstOrDefault(x => x.ProductID == product.ProductID);

            if (existing is null)
            {
                Items.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    Name = product.Name,
                    Category = product.Category,
                    Price = product.Price,
                    Quantity = 1
                });
            }
            else
            {
                existing.Quantity++;
            }

            NotifyStateChanged();
        }

        public void RemoveItem(long productId)
        {
            var item = Items.FirstOrDefault(x => x.ProductID == productId);
            if (item is not null)
            {
                Items.Remove(item);
                NotifyStateChanged();
            }
        }

        public void DecreaseItem(long productId)
        {
            var item = Items.FirstOrDefault(x => x.ProductID == productId);
            if (item is null) return;

            item.Quantity--;

            if (item.Quantity <= 0)
            {
                Items.Remove(item);
            }

            NotifyStateChanged();
        }

        public decimal GetTotal() => Items.Sum(x => x.Price * x.Quantity);

        public int GetCount() => Items.Sum(x => x.Quantity);

        public void Clear()
        {
            Items.Clear();
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }

    public class CartItem
    {
        public long ProductID { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductDto
    {
        public long ProductID { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
    }
}