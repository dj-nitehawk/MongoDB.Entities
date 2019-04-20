using System;
using Microsoft.AspNetCore.Mvc;
using DemoAPI.Models;

namespace DemoAPI.Controllers
{
    [ApiController]
    public class DemoController : ControllerBase
    {
        [HttpGet("demo")]
        public IActionResult Test()
        {
            //CREATE
            var person = new Person
            {
                Name = "Test " + DateTime.UtcNow.Ticks.ToString(),
                Age = 32,
                PhoneNumbers = new string[] { "123456", "654321", "555555" },
                RetirementDate = DateTime.UtcNow
            };

            person.Save();

            var address = new Address {
                Line1 = "line 1",
                City = "Colarado",
                OwnerId = person.ID };

            address.Save();

            //READ
            var lastPerson = person.FindLast();

            //UPDATE
            lastPerson.Name = "Updated at " + DateTime.UtcNow.ToString();
            lastPerson.Save();

            //DELETE
            //lastPerson.Delete();
            //address.DeleteByOwnerId(lastPerson.Id);

            return Ok("CRUD Complete...");
        }
    }
}