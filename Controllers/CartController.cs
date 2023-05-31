using CartMicroservice.DbContexts;
using CartMicroservice.Dto;
using CartMicroservice.Models;
using CartMicroservice.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace CartMicroservice.Controllers
{
    [Route("api/rest/v1/cart")]
    [ApiController]
    public class CartController : ControllerBase
    {

        private readonly CartMicroserviceDbContext _context;
        public readonly IApiService _api;

        public CartController(CartMicroserviceDbContext context , IApiService api)
        {
            _context = context;

            _api = api ??
                    throw new ArgumentNullException(nameof(api));
        }




        [HttpGet("items")]
        public async Task<IActionResult> GetItems()
        {

            // Retrieve the JWT token from the Authorization header
            var authorizationHeader = Request.Headers["Authorization"].ToString();
            var token = authorizationHeader.Replace("Bearer ", "");

            HttpResponseMessage authresponse = await _api.isAuthorized(token);

            if (!authresponse.IsSuccessStatusCode)
            {
                return Unauthorized();
            }


            Guid customerId = _api.getUserId(token);

            var records = _context.Cart.Where(x => x.CId ==customerId).Select(y => y.PId);

            if (!records.Any())
            {
                return Ok(new {items = records});
            }


            var ProductIdList = await records.ToListAsync();

            HttpResponseMessage response = await _api.getProductDetails(ProductIdList);


            if (response.IsSuccessStatusCode)
            {

            string content = await response.Content.ReadAsStringAsync();
            List<GetCartItemsDto>? CartItems = JsonSerializer.Deserialize<List<GetCartItemsDto>>(content);


            foreach (var i in CartItems)
            {
                var temp = _context.Cart.FirstOrDefault(x => x.CId == customerId && x.PId == i.pId);
                i.quantity = temp.Quantity;
                i.cartId = temp.CartId;
            }

            return Ok(new { items = CartItems });


            }

    
            return StatusCode(503, "Product Service is currently unavailable"); 
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

            HttpResponseMessage authresponse = await _api.isAuthorized(token);

            if (!authresponse.IsSuccessStatusCode)
            {
                return Unauthorized();
            }

            data.CId = _api.getUserId(token);



            var response = await _api.isValidProduct(data.PId);

            if (!response.IsSuccessStatusCode)
            {
                return NotFound();

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

            HttpResponseMessage response = await _api.isAuthorized(token);

            if (!response.IsSuccessStatusCode)
            {
                return Unauthorized();
            }

            Guid customerId = _api.getUserId(token);
;
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

            HttpResponseMessage response = await _api.isAuthorized(token);

            if (!response.IsSuccessStatusCode)
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

            HttpResponseMessage response = await _api.isAuthorized(token);

            if (!response.IsSuccessStatusCode)
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
