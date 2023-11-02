
using Iot.Device.Ssd13xx.Commands;
using Ssd1306Cmnds = Iot.Device.Ssd13xx.Commands.Ssd1306Commands;

namespace Iot.Device.Ssd13xx.Samples
{
    /// <summary>
    /// Extension methods for Ssd1306 class.
    /// </summary>
    public static class Ssd1306Extensions
    {
        internal static void Initialize(this Ssd1306 device)
        {
            device.SendCommand(new SetDisplayOff());
            device.SendCommand(new Ssd1306Cmnds.SetDisplayClockDivideRatioOscillatorFrequency(0x00, 0x08));
            device.SendCommand(new SetMultiplexRatio(0x1F));
            device.SendCommand(new Ssd1306Cmnds.SetDisplayOffset(0x00));
            device.SendCommand(new Ssd1306Cmnds.SetDisplayStartLine(0x00));
            device.SendCommand(new Ssd1306Cmnds.SetChargePump(true));
            device.SendCommand(
                new Ssd1306Cmnds.SetMemoryAddressingMode(Ssd1306Cmnds.SetMemoryAddressingMode.AddressingMode
                    .Horizontal));
            device.SendCommand(new Ssd1306Cmnds.SetSegmentReMap(true));
            device.SendCommand(new Ssd1306Cmnds.SetComOutputScanDirection(false));
            device.SendCommand(new Ssd1306Cmnds.SetComPinsHardwareConfiguration(false, false));
            device.SendCommand(new SetContrastControlForBank0(0x8F));
            device.SendCommand(new Ssd1306Cmnds.SetPreChargePeriod(0x01, 0x0F));
            device.SendCommand(
                new Ssd1306Cmnds.SetVcomhDeselectLevel(Ssd1306Cmnds.SetVcomhDeselectLevel.DeselectLevel.Vcc1_00));
            device.SendCommand(new Ssd1306Cmnds.EntireDisplayOn(false));
            device.SendCommand(new Ssd1306Cmnds.SetNormalDisplay());
            device.SendCommand(new SetDisplayOn());
            device.SendCommand(new Ssd1306Cmnds.SetColumnAddress());
            device.SendCommand(new Ssd1306Cmnds.SetPageAddress(Ssd1306Cmnds.PageAddress.Page1,
                Ssd1306Cmnds.PageAddress.Page3));

        }

        internal static void Clear(this Ssd1306 device)
        {
            // start from first column
            device.SendCommand(new Ssd1306Cmnds.SetColumnAddress());
            // work across all pages (rows)
            device.SendCommand(new Ssd1306Cmnds.SetPageAddress(Ssd1306Cmnds.PageAddress.Page0,
                Ssd1306Cmnds.PageAddress.Page3));

            for (int cnt = 0; cnt < 32; cnt++)
            {
                byte[] data = new byte[16];
                device.SendData(data);
            }        
        }       

        internal static void SetCursorPosition(this Ssd1306 device, int left, int top)
        {
            const int TextWidthInPixel = 16;

            // valid left => 0 - 15
            if (left >= 0 && left <= 15)
            {
                device.SendCommand(new Ssd1306Cmnds.SetColumnAddress(
                    (byte)(left * TextWidthInPixel)));
            }

            // valid top => 0 - 3
            if (top >= 0 && top <= 3)
            {
                device.SendCommand(new Ssd1306Cmnds.SetPageAddress(
                    (Ssd1306Cmnds.PageAddress)top,
                    Ssd1306Cmnds.PageAddress.Page3));
            }
        }

        internal static void Write(this Ssd1306 device, string message)
        {
            foreach (char character in message)
            {
                device.SendData(BasicFont.GetCharacterBytes(character));
            }
        }
    }
}