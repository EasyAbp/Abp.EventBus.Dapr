using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc;


namespace App1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : AbpController
    {

        public ValuesController()
        {
   
        }

        [HttpGet]
        public async Task<string> AppMessage()
        {
           return "";
        }        
    }
}