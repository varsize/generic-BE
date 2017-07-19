using System.Collections.Generic;
using System.Net.Sockets;
using System.Web.Http;
using BlockExplorerAPI.Services;
using BlockExplorerAPI.Services.Models;
using BlockExplorerAPI.Validation;

namespace BlockExplorerAPI.Controllers
{
    [RoutePrefix("address")]
    public class AddressController : BaseApiController
    {
        private readonly AddressService addressService;

        public AddressController(AddressService addressService)
        {
            this.addressService = addressService;
        }

        /// <summary>
        /// Get detailed info about address(es)
        /// </summary>
        /// <param name="search">One or more addresses separated by comma</param>
        /// <returns></returns>
        [HttpGet]
        [Route("info/{search}")]
        public IHttpActionResult Info(string search)
        {
            string[] searchParams = search.Trim().Split(',');

            var addresses = new List<AddressModel>();
            foreach (var searchParam in searchParams)
            {
                if (AddressValidator.Validate(searchParam))
                {
                    var address = addressService.Find(searchParam);
                    if (address != null) addresses.Add(address);
                }
            }

            if (addresses.Count == 0)
                return NotFound();
            return Ok(addresses);
        }

        [HttpGet]
        [Route("unspent/{address}")]
        public IHttpActionResult Unspent(string address)
        {
            if (!AddressValidator.Validate(address))
                return BadRequest("Invalid address");

            var unspentOutputs = addressService.FindUnspendWithPubKey(address);
            return Ok(unspentOutputs);
        }
    }
}
