namespace OrderService.DTOs;

public class OrderPageDto
{
    public List<OrderDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
