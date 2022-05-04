using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Iot.Device.Max7219;

namespace FP.ContainerTraining.RaspiLedMatrix.Business
{
    public class MatrixGraphics
    {
        private readonly MatrixRepository _matrixRepository;

        public MatrixGraphics(MatrixRepository matrixRepository, IFont font)
        {
            _matrixRepository = matrixRepository ?? throw new ArgumentNullException(nameof(matrixRepository));
            Font = font ?? throw new ArgumentNullException(nameof(font));
        }

        public IFont Font { get; set; }

        /// <summary>
        /// Shows a message on the device.
        /// If it's longer then the total width (or <see paramref="alwaysScroll"/> == true),
        /// it transitions the text message across the devices from right-to-left.
        /// </summary>
        public void ShowMessage(string text, CancellationToken stoppingToken, int delayInMilliseconds = 50, bool alwaysScroll = false)
        {
            IEnumerable<IReadOnlyList<byte>> textCharBytes = text.Select(chr => Font[chr]);
            int textBytesLength = textCharBytes.Sum(x => x.Count) + text.Length - 1;

            bool scroll = alwaysScroll || textBytesLength > _matrixRepository.Length;
            if (scroll)
            {
                var pos = _matrixRepository.Length - 1;
                _matrixRepository.ClearAll(false);
                foreach (var arr in textCharBytes)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }

                    foreach (byte b in arr)
                    {
                        ScrollLeft(b, true);
                        Thread.Sleep(delayInMilliseconds);

                    }

                    ScrollLeft(0, true);
                    Thread.Sleep(delayInMilliseconds);

                }

                for (; pos > 0; pos--)
                {
                    ScrollLeft(0, true);
                    Thread.Sleep(delayInMilliseconds);
                }
            }
            else
            {
                // calculate margin to display text centered
                var margin = (_matrixRepository.Length - textBytesLength) / 2;
                _matrixRepository.ClearAll(false);
                var pos = margin;
                foreach (var arr in textCharBytes)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        return;
                    }

                    foreach (byte b in arr)
                    {
                        _matrixRepository[pos++] = b;
                    }

                    pos++;
                }

                _matrixRepository.Flush();
            }
        }
        public void ScrollLeft(byte value, bool flush = true)
        {
            for (var i = 1; i < _matrixRepository.Length; i++)
            {
                _matrixRepository[i - 1] = _matrixRepository[i];
            }

            _matrixRepository[_matrixRepository.Length - 1] = value;
            if (flush)
            {
                _matrixRepository.Flush();
            }
        }
    }

   
}

