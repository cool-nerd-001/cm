using System.ComponentModel.DataAnnotations;

namespace CartMicroservice.Models
{
    public class Cart
    {
        [Key]
        public Guid CartId { get; set; }

        [Required]
        public Guid CId { get; set; }

        [Required]
        public Guid PId { get; set; }


        [Range(1, Int32.MaxValue, ErrorMessage = "Quantity Field is Required Or Quantity Should be a Positive Number")]
        public int Quantity { get; set; }


    }
}
