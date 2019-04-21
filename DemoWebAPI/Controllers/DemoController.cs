using System;
using Microsoft.AspNetCore.Mvc;
//using DemoAPI.Models;

namespace DemoAPI.Controllers
{
    [ApiController]
    public class DemoController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Test()
        {
            //todo: copy from console app
            return Ok("CRUD Complete...");
        }
    }
}