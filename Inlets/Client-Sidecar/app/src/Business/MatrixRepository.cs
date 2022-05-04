using System;
using Iot.Device.Max7219;

namespace FP.ContainerTraining.RaspiLedMatrix.Business
{
    public class MatrixRepository
    {

        public const int NumDigits = 8;

        public MatrixRepository()
        {
            CascadedDevices = 1;
            _buffer = new byte[CascadedDevices, NumDigits];
            CurrentBuffer = new byte[CascadedDevices, NumDigits];
            Rotation = RotationType.None;
        }

        public RotationType Rotation { get; set; }

        public Color Color { get; set; }

        public string Text { get; set; }

        public int CascadedDevices { get; set; }

        public int Length => CascadedDevices * NumDigits;

        private readonly byte[,] _buffer;

        public byte[,] CurrentBuffer { get; }
        public RotationType CurrentRotation { get; private set; }

        public byte this[int deviceId, int digit]
        {
            get
            {
                ValidatePosition(deviceId, digit);
                return _buffer[deviceId, digit];
            }
            set
            {
                ValidatePosition(deviceId, digit);
                _buffer[deviceId, digit] = value;
            }
        }

        public byte this[int index]
        {
            get
            {
                ValidateIndex(index, out var deviceId, out var digit);
                return _buffer[deviceId, digit];
            }
            set
            {
                ValidateIndex(index, out var deviceId, out var digit);
                _buffer[deviceId, digit] = value;
            }
        }

        public void Flush()
        {
            for (int device = 0; device < CascadedDevices; device++)
            {
                for (int i = 0; i < NumDigits; i++)
                {
                    CurrentBuffer[device, i] = _buffer[device, i];
                }
            }
            CurrentRotation = Rotation;

        }

        public void ClearAll(bool flush = true) => Clear(0, CascadedDevices, flush);

        public void Clear(int start, int end, bool flush = true)
        {
            if (end < 0 || end > CascadedDevices)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            if (start < 0 || start >= end)
            {
                throw new ArgumentOutOfRangeException(nameof(end));
            }

            for (var deviceId = start; deviceId < end; deviceId++)
            {
                for (var digit = 0; digit < NumDigits; digit++)
                {
                    this[deviceId, digit] = 0;
                }
            }

            if (flush)
            {
                Flush();
            }
        }

        private void ValidatePosition(int deviceId, int digit)
        {
            if (deviceId < 0 || deviceId >= CascadedDevices)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId), $"Invalid device Id: {deviceId}");
            }

            if (digit < 0 || digit >= NumDigits)
            {
                throw new ArgumentOutOfRangeException(nameof(digit), $"Invalid digit: {digit}");
            }
        }

        private void ValidateIndex(int index, out int deviceId, out int digit)
        {
            if (index < 0 || index > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}");
            }

            deviceId = Math.DivRem(index, NumDigits, out digit);
        }
    }
}
