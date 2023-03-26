namespace CartMicroservice.Dto
{
    public class GetCartItemsDto
    {
        public Guid cartId { get; set; }
        public Guid pId { get; set; }

        public string? name { get; set; }

        public int price { get; set; }

        public string? image {get; set; }

        public int quantity { get; set; }

    }
}
