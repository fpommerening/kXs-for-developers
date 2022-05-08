using System.Threading;
using System.Threading.Tasks;
using FP.ContainerTraining.RaspiLedMatrix.Business;
using Iot.Device.Max7219;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.RaspiLedMatrix.Services
{
    public class TextWriterService : BackgroundService
    {
        private readonly ILogger<TextWriterService> _logger;
        private readonly MatrixRepository _matrixRepository;

        public TextWriterService(ILogger<TextWriterService> logger, MatrixRepository matrixRepository)
        {
            _logger = logger;
            _matrixRepository = matrixRepository;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!string.IsNullOrEmpty(_matrixRepository.Text))
                {
                    var matrixGraphics = new Business.MatrixGraphics(_matrixRepository, Fonts.CP437);
                    matrixGraphics.ShowMessage(_matrixRepository.Text, stoppingToken, alwaysScroll: true, delayInMilliseconds:80);
                }

                await Task.Delay(500, stoppingToken);

            }
        }
    }
}
