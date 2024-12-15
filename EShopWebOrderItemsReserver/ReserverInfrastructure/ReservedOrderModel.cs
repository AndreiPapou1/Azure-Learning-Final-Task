namespace ReserverInfrastructure
{
    public class ReservedOrderModel
    {
        public string id { get; set; }
        public decimal TotalPrice { get; set; }
        public object ShippingAddress { get; set; }
        public List<object> Items { get; set; }
        public string BuyerId { get; set; }

    }
}
