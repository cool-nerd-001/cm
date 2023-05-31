

namespace CartMicroservice.Services
{
    public interface IApiService
    {

        public Task<HttpResponseMessage> isAuthorized(string token);

        public Guid getUserId(string token);

        public Task<HttpResponseMessage> isValidProduct(Guid PId);

        public Task<HttpResponseMessage> getProductDetails(List<Guid> ProductIdList);
    }
}
