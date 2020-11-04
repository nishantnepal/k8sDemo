﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace K8sDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static bool Is_Live = true;
        private static bool Is_Ready_To_Receive_Traffic = true;
        private Random _random;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
            _random = new Random();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var productsUrl = _configuration["ProductBaseUrl"];
            var productsHost = new Uri(productsUrl).Host;
            var appEnv = _configuration["AppEnvironment"];
            var loggingName = _configuration["GtzLoggerFactorySettings:System"];
            string ipAddress = null;
            try
            {
                ipAddress = (await Dns.GetHostAddressesAsync(productsHost))?.FirstOrDefault().ToString();
            }
            catch (Exception ex)
            {
                ipAddress = $"None - {ex.Message}";
            }

            var response = new
            {
                ProductsIpAddress = ipAddress,
                AppEnv = appEnv,
                LoggingName = loggingName,
                Version = "V1"
            };

            return Ok(response);
        }

        [HttpGet("live")]
        public IActionResult HealthLive()
        {
            if (Is_Live)
            {
                return Ok($"{Environment.MachineName} - Is_Live: {Is_Live}");
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("ready")]
        public IActionResult HealthReady()
        {
            if (Is_Ready_To_Receive_Traffic)
            {
                return Ok($"{Environment.MachineName} - Is_Ready_To_Receive_Traffic: {Is_Ready_To_Receive_Traffic}");
            }

            return StatusCode(StatusCodes.Status429TooManyRequests);
        }

        [HttpGet("live/mutate")]
        public IActionResult HealthReadyLive()
        {
            Is_Live = false;
            return Ok($"{Environment.MachineName} - Is_Live: {Is_Live}");
        }

        [HttpGet("ready/mutate")]
        public IActionResult ReadyMutate()
        {
            Is_Ready_To_Receive_Traffic = false;
            return Ok($"{Environment.MachineName} -  Is_Ready_To_Receive_Traffic: {Is_Ready_To_Receive_Traffic}");
        }

        [HttpGet("cpuSpike")]
        public IActionResult CpuSpike()
        {
            Enumerable
                .Range(1, Environment.ProcessorCount - 1) // replace with lesser number if 100% usage is not what you are after.
                .AsParallel()
                .Select(i =>
                {
                    var end = DateTime.Now + TimeSpan.FromSeconds(10);
                    while (DateTime.Now < end)
                        /*nothing here */
                        ;
                    return i;
                })
                .ToList();
            return Ok("Done");
        }

        [HttpGet("memorySpike")]
        public IActionResult MemorySpike(int mbToConsume = 256)
        {
            
            List<XmlNode> memList = new List<XmlNode>();
            long mbConsumed = 0;

            while (mbConsumed <= (mbToConsume * 1000000))
            {
                long preTotal = GC.GetTotalMemory(false);
                XmlDocument doc = new XmlDocument();
                for (int i = 0; i < 1000000; i++)
                {
                    XmlNode x = doc.CreateNode(XmlNodeType.Element, "hello", "");
                    memList.Add(x);
                }
                long postTotal = GC.GetTotalMemory(false);
                mbConsumed += postTotal - preTotal;
                preTotal = postTotal;
                Console.WriteLine("Memory Usage:" + mbConsumed);
                Thread.Sleep(2 * 1000);
            }
            return Ok("Done");
        }

        private int GetObjectSize(XmlDocument testObject)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, testObject);
            var array = ms.ToArray();
            return array.Length;
        }
    }
}