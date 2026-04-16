namespace SportsStore.Models
{
    public enum OrderStatus
    {
        Cart = 0,
        Submitted = 1,
        InventoryPending = 2,
        InventoryConfirmed = 3,
        InventoryFailed = 4,
        PaymentPending = 5,
        PaymentApproved = 6,
        PaymentFailed = 7,
        ShippingPending = 8,
        ShippingCreated = 9,
        Completed = 10,
        Failed = 11
    }
}