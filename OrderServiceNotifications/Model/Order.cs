namespace OrderServiceNotifications.Model;

using System.Text.Json.Serialization;

public class Order
{
    public Guid Id { get; set; }

    public string CustomerEmail { get; set; } = null!;

    public string ProductCode { get; set; } = null!;

    public int Quantity { get; set; }

    public string Status { get; set; } = "CREATED";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
