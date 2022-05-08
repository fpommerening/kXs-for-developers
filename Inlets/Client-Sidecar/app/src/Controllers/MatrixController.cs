using System;
using FP.ContainerTraining.RaspiLedMatrix.Business;
using Iot.Device.Max7219;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.RaspiLedMatrix.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MatrixController : ControllerBase
    {

        private readonly ILogger<MatrixController> _logger;
        private readonly MatrixRepository _repository;

        public MatrixController(ILogger<MatrixController> logger, MatrixRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        [HttpPut("~/text")]
        public IActionResult StartText([FromBody] TextRequest request)
        {
            _repository.Graphic = string.Empty;
            _repository.Text = request.Text;
            _repository.Rotation = RotationType.Left;
            return Accepted();
        }

        [HttpPut("~/graphics/{val}")]
        public IActionResult StartGraphics([FromRoute] string val)
        {
            _repository.Text = string.Empty;
            _repository.Graphic = val;
            _repository.Rotation = RotationType.Left;
            return Accepted();
        }

        [HttpGet("~/color")]
        public string GetColor()
        {
            return _repository.Color.ToString();
        }

        [HttpPut("~/color/{val}")]
        public IActionResult SetColor([FromRoute] string val)
        {
            if (Enum.TryParse(typeof(Color), val, true, out var color))
            {
                _repository.Color = (Color)color;
                
                return Accepted();
            }
            return BadRequest($"Color {val} is not supported");
        }

        [HttpPut("~/rotation/{val}")]
        public IActionResult SetRotation([FromRoute] string val)
        {
            if (Enum.TryParse(typeof(RotationType), val, true, out var rotation))
            {
                _repository.Rotation = (RotationType)rotation;
                return Accepted();
            }
            return BadRequest($"RotationType {val} is not supported");
        }
    }
}
