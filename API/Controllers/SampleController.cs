﻿using Microsoft.AspNetCore.Mvc;
using RabbitMQUtils;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SampleController : Controller
    {
        readonly RabbitMqConnectionManager _connectionManager;
        readonly RabbitMqSettingsManager _settingsManager;

        public SampleController(RabbitMqConnectionManager connectionManager, RabbitMqSettingsManager settingsManager)
        {
            _connectionManager = connectionManager;
            _settingsManager = settingsManager;
        }
        [HttpGet("PublishLoginMessage")]
        public async Task<IActionResult> PublishLoginMessageAsync(string message)
        {
            try
            {
                var requester = new RabbitMQUtils.RequestResponse.ReqResBuilder().WithConnection(_connectionManager)
                .WithSettings(_settingsManager, "user.login")
                .WithEvent(async (corId, message) => {
                    var content = $"values: corID:{corId}, message:{message}";
                    Console.WriteLine(content);
                    return content;
                }).CreateRequester();
                await requester.RequestAndWaitAsync(message);
                return Ok(requester.RequesterResult);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("PublishRegistrationMessage")]
        public async Task<IActionResult> PublishRegistrationMessageAsync(string message)
        {
            try
            {
                var requester = new RabbitMQUtils.RequestResponse.ReqResBuilder().WithConnection(_connectionManager)
                .WithSettings(_settingsManager, "user.registration")
                .WithEvent(async (corId, message) => {
                    var content = $"values: corID:{corId}, message:{message}";
                    Console.WriteLine(content);
                    return content;
                }).CreateRequester();
                await requester.RequestAndWaitAsync(message);
                return Ok(requester.RequesterResult);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
