using CartMicroservice.DbContexts;
using CartMicroservice.Dto;
using CartMicroservice.Models;
using CartMicroservice.Utils;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;


namespace CartMicroservice.Controllers
{
    [Route("api/rest/v1/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {

        private readonly CartMicroserviceDbContext _context;
        public readonly IConfiguration _configuration;

        public CartController(CartMicroserviceDbContext context , IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration ??
                    throw new ArgumentNullException(nameof(configuration));
        }




        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {

            // Retrieve the JWT token from the Authorization header
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Replace("Bearer ", "");

            Helper h = new Helper(_configuration);

            var flag = await h.isAuthorised(token);

            if (!flag)
            {
                return Unauthorized();
            }

            Guid customerId = h.getUserId(token);





            var records = _context.Cart.Where(x => x.CId ==customerId).Select(y => y.PId);

            if (!records.Any())
            {
                return Ok(new {items = records});
            }


            var ProductIdList = new { array = records};

            using (var client = new HttpClient())
            {
                string? domin = _configuration["ProductMicroservice:domin"];
                client.BaseAddress = new Uri(domin);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.PostAsJsonAsync("/api/rest/v1/cartitems", ProductIdList).Result;



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

                    return Ok(new { items = CartItems });


                }
    
            }


            return Ok(new {items = records});
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddtoCart(AddToCartDto data)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            // Retrieve the JWT token from the Authorization header
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Replace("Bearer ", "");

            Helper h = new Helper(_configuration);

            var flag = await h.isAuthorised(token);

            if (!flag)
            {
                return Unauthorized();
            }

            data.CId = h.getUserId(token);



 
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

        [HttpDelete("clearAll")]
        public async Task<IActionResult> ClearCustomerCart()
        {

            // Retrieve the JWT token from the Authorization header
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Replace("Bearer ", "");

            Helper h = new Helper(_configuration);

            var flag = await h.isAuthorised(token);

            if (!flag)
            {
                return Unauthorized();
            }

            Guid customerId = h.getUserId(token);

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

            // Retrieve the JWT token from the Authorization header
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Replace("Bearer ", "");

            Helper h = new Helper(_configuration);

            var flag = await h.isAuthorised(token);

            if (!flag)
            {
                return Unauthorized();
            }

            var record = await _context.Cart.FindAsync(cartId);

            if (record == null)
            {
                return NotFound();
            }

            _context.Cart.Remove(record);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("reduce/{cartId:guid}")]
        public async Task<IActionResult> ReduceQuantity([FromRoute] Guid cartId)
        {

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Replace("Bearer ", "");

            Helper h = new Helper(_configuration);

            var flag = await h.isAuthorised(token);

            if (!flag)
            {
                return Unauthorized();
            }

            var record = await _context.Cart.FindAsync(cartId);

            if (record == null)
            {
                return NotFound();
            }

            if(record.Quantity == 1)
            {
                _context.Cart.Remove(record);
                await _context.SaveChangesAsync();

                return Ok();

            }

            record.Quantity = record.Quantity - 1;

            await _context.SaveChangesAsync();

            return Ok();
        }



    }
}
