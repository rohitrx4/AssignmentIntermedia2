using System.ComponentModel.DataAnnotations;

namespace OrderServiceNotifications.Contracts
{
    public class CreateOrderRequest
    {
        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = null!;

        [Required]
        public string ProductCode { get; set; } = null!;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
