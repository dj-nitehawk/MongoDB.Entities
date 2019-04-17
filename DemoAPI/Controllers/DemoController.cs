using System;
using Microsoft.AspNetCore.Mvc;
using DemoAPI.Models;

namespace DemoAPI.Controllers
{
    [ApiController]
    public class DemoController : ControllerBase
    {
        [HttpGet("demo")]
        public ActionResult<Person> Test()
        {
            var person = new Person
            {
                Name = "Test " + DateTime.UtcNow.Ticks.ToString(),
                Age = 32,
                PhoneNumbers = new string[] { "123456", "654321", "555555" }
            };

            person.Save();

            return Ok(person);
        }
    }
}