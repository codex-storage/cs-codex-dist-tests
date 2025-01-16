using Microsoft.AspNetCore.Mvc;
using Utils;

namespace KeyMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KeyController : ControllerBase
    {
        [HttpGet]
        public KeyResponse Get()
        {
            var account = EthAccount.GenerateNew();

            return new KeyResponse
            {
                Public = account.EthAddress.Address,
                Private = account.PrivateKey,
                Secure = "Not Secure! For demo/development purposes only!"
            };
        }
    }
}
