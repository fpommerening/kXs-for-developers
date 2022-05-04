using System.Device.Spi;
using System.Threading.Tasks;
using Iot.Device.Max7219;

namespace FP.ContainerTraining.RaspiLedMatrix.Business;
public class MatrixRunner
{
    private readonly MatrixRepository _matrixRepository;

    public MatrixRunner(MatrixRepository matrixRepository)
    {
        _matrixRepository = matrixRepository;
        _writeBuffer = new byte[4 * _matrixRepository.CascadedDevices];
    }

    private readonly byte[] _writeBuffer;

    public void Run()
    {
        Task.Run(() =>
        {
            var busId = 0;
            var chipSelectLine = -1;

            var settings = new SpiConnectionSettings(busId, chipSelectLine);
            settings.ClockFrequency = 10_000_00;
            var device = SpiDevice.Create(settings);

            do
            {
                if (_matrixRepository.Color == Color.None)
                {
                    System.Threading.Thread.Sleep(2_000);
                }

                switch (_matrixRepository.CurrentRotation)
                {
                    case RotationType.None:
                        WriteBufferWithoutRotation(device);
                        break;
                    case RotationType.Half:
                        WriteBufferRotateHalf(device);
                        break;
                    case RotationType.Right:
                        WriteBufferRotateRight(device);
                        break;
                    case RotationType.Left:
                        WriteBufferRotateLeft(device);
                        break;
                }
                    
            } while (true);
        });
    }

    private void WriteBufferWithoutRotation(SpiDevice device)
    {

        for (var digit = 0; digit < MatrixRepository.NumDigits; digit++)
        {
            var i = 0;
            for (var deviceId = _matrixRepository.CascadedDevices - 1; deviceId >= 0; deviceId--)
            {
                var shiftValue = 0x01 << digit;

                _writeBuffer[i++] = _matrixRepository.Color == Color.Red
                    ? (byte) ~_matrixRepository.CurrentBuffer[deviceId, digit]
                    : (byte) 0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Blue
                    ? (byte) ~_matrixRepository.CurrentBuffer[deviceId, digit]
                    : (byte) 0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Green
                    ? (byte) ~_matrixRepository.CurrentBuffer[deviceId, digit]
                    : (byte) 0xFF;

                _writeBuffer[i++] = (byte) shiftValue;
            }

            device.Write(_writeBuffer);
            System.Threading.Thread.Sleep(2);
        }
    }

    private void WriteBufferRotateHalf(SpiDevice device)
    {
        for (var digit = 0; digit < MatrixRepository.NumDigits; digit++)
        {
            var i = 0;
            for (var deviceId = _matrixRepository.CascadedDevices - 1; deviceId >= 0; deviceId--)
            {
                var shiftValue = 0x01 << digit;

                var b = _matrixRepository.CurrentBuffer[deviceId, 7 - digit];
                // reverse bits in byte
                b = (byte)((b * 0x0202020202 & 0x010884422010) % 1023);

                _writeBuffer[i++] = _matrixRepository.Color == Color.Red
                    ? (byte)~b
                    : (byte)0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Blue
                    ? (byte)~b
                    : (byte)0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Green
                    ? (byte)~b
                    : (byte)0xFF;

                _writeBuffer[i++] = (byte)shiftValue;
            }
            device.Write(_writeBuffer);
            System.Threading.Thread.Sleep(2);
        }
    }

    private void WriteBufferRotateRight(SpiDevice device)
    {
        for (var digit = 0; digit < MatrixRepository.NumDigits; digit++)
        {
            var mask = 0x01 << digit;
            var i = 0;
            for (var deviceId = _matrixRepository.CascadedDevices - 1; deviceId >= 0; deviceId--)
            {
                var shiftValue = 0x01 << digit;

                byte value = 0;
                byte targetBit = 0x80;
                for (int bitDigit = 0; bitDigit < MatrixRepository.NumDigits; bitDigit++, targetBit >>= 1)
                {
                    if ((_matrixRepository.CurrentBuffer[deviceId, bitDigit] & mask) != 0)
                    {
                        value |= targetBit;
                    }
                }
                _writeBuffer[i++] = _matrixRepository.Color == Color.Red
                    ? (byte)~value
                    : (byte)0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Blue
                    ? (byte)~value
                    : (byte)0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Green
                    ? (byte)~value
                    : (byte)0xFF;

                _writeBuffer[i++] = (byte)shiftValue;
            }
            device.Write(_writeBuffer);
            System.Threading.Thread.Sleep(2);
        }
    }

    private void WriteBufferRotateLeft(SpiDevice device)
    {
        for (var digit = 0; digit < MatrixRepository.NumDigits; digit++)
        {
            var mask = 0x80 >> digit;
            var i = 0;
            for (var deviceId = _matrixRepository.CascadedDevices - 1; deviceId >= 0; deviceId--)
            {
                var shiftValue = 0x01 << digit;

                byte value = 0;
                byte targetBit = 0x80;
                for (int bitDigit = 0; bitDigit < MatrixRepository.NumDigits; bitDigit++, targetBit >>= 1)
                {
                    if ((_matrixRepository.CurrentBuffer[deviceId, bitDigit] & mask) != 0)
                    {
                        value |= targetBit;
                    }
                }
                _writeBuffer[i++] = _matrixRepository.Color == Color.Red
                    ? (byte)~value
                    : (byte)0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Blue
                    ? (byte)~value
                    : (byte)0xFF;
                _writeBuffer[i++] = _matrixRepository.Color == Color.Green
                    ? (byte)~value
                    : (byte)0xFF;

                _writeBuffer[i++] = (byte)shiftValue;
            }
            device.Write(_writeBuffer);
            System.Threading.Thread.Sleep(2);
        }
    }
}