using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

namespace CartMicroservice.Utils
{
    public class Helper
    {

        private readonly IConfiguration _configuration;

        public Helper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Boolean> isAuthorised(string token)
        {
            using (var client = new HttpClient())
            {
                string? domin = _configuration["ActiveDirectoryMicroservice:domin"];
                client.BaseAddress = new Uri(domin);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("/api/rest/v1/validate/user/");

                if (response.IsSuccessStatusCode)
                {
                    return true;

                }


            }

            return false;

        }

        public Guid getUserId(string token)
        {

            // Parse the JWT token and extract the claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claims = jwtToken.Claims;

            // Access the claims
            var Id = claims.FirstOrDefault(c => c.Type == "Id")?.Value;

            return new Guid(Id);

        }

    }
}
