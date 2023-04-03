using System.ComponentModel.DataAnnotations;

namespace CartMicroservice.Dto
{
    public class AddToCartDto
    {
        public Guid CId { get; set; }

        [Required]
        public Guid PId { get; set; }
    }
}
