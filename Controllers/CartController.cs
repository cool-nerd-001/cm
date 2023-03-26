using CartMicroservice.DbContexts;
using CartMicroservice.Dto;
using CartMicroservice.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;


namespace CartMicroservice.Controllers
{
    [Route("api/rest/v1/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {

        private readonly CartMicroserviceDbContext _context;
        private readonly IConfiguration _configuration;

        public CartController(CartMicroserviceDbContext context , IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration ??
                    throw new ArgumentNullException(nameof(configuration));
        }


        [HttpGet("items/{customerId:guid}")]
        public async Task<IActionResult> GetItems([FromRoute] Guid customerId)
        {
            var records = _context.Cart.Where(x => x.CId ==customerId).Select(y => y.PId);

            if (!records.Any())
            {
                return Ok(new {array = records});
            }


            var ProductIdList = new { array = records};

            using (var client = new HttpClient())
            {
                string? domin = _configuration["ProductMicroservice:domin"];
                client.BaseAddress = new Uri(domin);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PostAsJsonAsync("/api/rest/v1/productdetails/cartitems", ProductIdList).Result;



                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var CartItems = JsonSerializer.Deserialize<IList<GetCartItemsDto>>(content);


                    foreach (var i in CartItems)
                    {
                        var temp = _context.Cart.FirstOrDefault(x => x.CId == customerId && x.PId == i.pId);
                        i.quantity = temp.Quantity;
                        i.cartId = temp.CartId;
                    }

                    return Ok(CartItems);


                }
    
            }


            return Ok(new {array = records});
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddtoCart(AddToCartDto data)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            
            using (var client = new HttpClient())
            {
                string? domin = _configuration["ProductMicroservice:domin"];
                client.BaseAddress = new Uri(domin);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("/api/rest/v1/verify/" + data.PId);

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();

                }

            }


            var record = _context.Cart.FirstOrDefault(x => x.CId == data.CId 
                                                      && x.PId == data.PId);
            if(record != null)
            {
                record.Quantity = record.Quantity + 1;
                await _context.SaveChangesAsync();

                return Ok();
            }


            Cart new_record = new Cart()
            {
                CartId = Guid.NewGuid(),
                CId = data.CId,
                PId = data.PId,
                Quantity = 1
            };
            await _context.Cart.AddAsync(new_record);
            await _context.SaveChangesAsync();

            return Ok();


        }

        [HttpDelete("clearAll/{customerId:guid}")]
        public async Task<IActionResult> ClearCustomerCart([FromRoute] Guid customerId)
        {


      
            var record = _context.Cart.FirstOrDefault(x => x.CId == customerId);

            if (record == null)
            {
                return NotFound();
            }


            var records = _context.Cart.Where(x => x.CId == customerId);
            _context.Cart.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("delete/{cartId:guid}")]
        public async Task<IActionResult> DeleteCartItem([FromRoute] Guid cartId)
        {

            var record = await _context.Cart.FindAsync(cartId);

            if (record == null)
            {
                return NotFound();
            }

            _context.Cart.Remove(record);
            await _context.SaveChangesAsync();

            return Ok();
        }



    }
}
