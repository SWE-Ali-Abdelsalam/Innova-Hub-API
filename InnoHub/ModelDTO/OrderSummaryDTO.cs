namespace InnoHub.ModelDTO
{
    public class OrderSummaryDTO
    {
        public decimal Subtotal { get; set; }
        public decimal ShippingDeliveryMethod { get; set; }
        public decimal Taxes { get; set; }
        public decimal Total { get; set; }
        public void RoundValues()
        {
            Subtotal = Math.Round(Subtotal, 2);
            ShippingDeliveryMethod = Math.Round(ShippingDeliveryMethod, 2);
            Taxes = Math.Round(Taxes, 2);
            Total = Math.Round(Total, 2);
        }

    }
}
