using System.Device.Spi;
using System.Resources;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FP.ContainerTraining.RaspiLedMatrix.Business
{
    public class MatrixRunner
    {
        private Color _color = Color.None;
        private Speed _speed = Speed.None;

        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    Clear = true;
                }

            } 
        }

        public Speed Speed
        {
            get => _speed;
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    Clear = true;
                }
            }
        }

        private bool Clear { get; set; }

        public void Run()
        {
            Task.Run(() =>
            {
                var busId = 0;
                var chipSelectLine = -1;

                var settings = new SpiConnectionSettings(busId, chipSelectLine);
                settings.ClockFrequency = 1000000;
                SpiDevice device = SpiDevice.Create(settings);

                do
                {
                    Clear = false;
                    Paint(device);
                    Off(device);
                } while (true);
            });
        }

        private void Paint(SpiDevice device)
        {

            byte[] picture = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            for (var row = 0; row < 8; row++)
            {
                for (var col = 7; col >= 0; col--)
                {
                    if (Clear)
                    {
                        return;
                    }

                    var val = 0xFF >> col;
                    picture[row] = (byte)val;

                    for (var picrepeat = (int) Speed; picrepeat > 0; picrepeat--)
                    {
                        for (int shift = 0; shift < 8; shift++)
                        {
                            var shiftValue = 0x01 << shift;

                            var data = new[]
                            {
                                Color == Color.Red ? (byte)~picture[shift] : (byte)0xFF,
                                Color == Color.Blue ? (byte)~picture[shift] : (byte)0xFF,
                                Color == Color.Green ? (byte)~picture[shift] : (byte)0xFF,
                                (byte)shiftValue
                            };

                            device.Write(data);

                            System.Threading.Thread.Sleep(2);
                        }
                    }
                }
            }
        }

        void Off(SpiDevice device)
        {
            byte j;
            for (j = 0; j < 8; j++)
            {
                var l = 0x01 << j;
                var data = new byte[]
                {
                    0xFF,
                    0xFF,
                    0xFF,
                    (byte)l
                };

                device.Write(data);

                System.Threading.Thread.Sleep(2);
            }
        }
    }
}
