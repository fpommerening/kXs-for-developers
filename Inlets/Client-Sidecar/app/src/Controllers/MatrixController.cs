using System;
using FP.ContainerTraining.RaspiLedMatrix.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.RaspiLedMatrix.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MatrixController : ControllerBase
    {

        private readonly ILogger<MatrixController> _logger;
        private readonly MatrixRunner _runner;

        public MatrixController(ILogger<MatrixController> logger, MatrixRunner runner)
        {
            _logger = logger;
            _runner = runner;
        }

        [HttpGet("~/speed")]
        public string  GetSpeed()
        {
            return _runner.Speed.ToString();
        }

        [HttpPut("~/speed/{val}")]
        public IActionResult SetSpeed([FromRoute]string val)
        {
            if (Enum.TryParse(typeof(Speed), val, true, out var speed))
            {
                _runner.Speed = (Speed)speed;
                return Accepted();
            }
            return BadRequest($"Speed {val} is not supported");
        }

        [HttpGet("~/color")]
        public string GetColor()
        {
            return _runner.Color.ToString();
        }

        [HttpPut("~/color/{val}")]
        public IActionResult SetColor([FromRoute] string val)
        {
            if (Enum.TryParse(typeof(Color), val, true, out var color))
            {
                _runner.Color = (Color)color;
                return Accepted();
            }
            return BadRequest($"Color {val} is not supported");
        }
    }
}
