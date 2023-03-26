using CartMicroservice.Models;
using Microsoft.EntityFrameworkCore;

namespace CartMicroservice.DbContexts
{
    public class CartMicroserviceDbContext : DbContext
    {
        public CartMicroserviceDbContext(DbContextOptions<CartMicroserviceDbContext> options) : base(options)
        {

        }


        public DbSet<Cart> Cart { get; set; }
    }
}
