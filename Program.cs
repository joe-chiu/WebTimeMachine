// Web Time Machine, inspired by this video https://www.youtube.com/watch?v=0OB1g8CUdbA
// Implemented using Orange Pi Zero 2 SBC and C#/.Net IoT
// The program allows a user to change Wayback Machine proxy content dates through a dial
// The project depends on Wayback Machine Proxy: https://github.com/richardg867/WaybackProxy
// The Linux running on the Orange Pi Zero 2 SBC is configured as a masquerade transparent proxy
// as discussed in the referenced Youtube video above

using System.Device.Gpio;
using System.Device.I2c;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Iot.Device.Button;
using Iot.Device.CharacterLcd;
using Iot.Device.Gpio.Drivers;
using Iot.Device.RotaryEncoder;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Samples;

// HW doc: https://drive.google.com/file/d/1T6ZbnrYIEGc89uKukrdOw8ESX8CYfmTY/view page 139
// These values are specific to Orange Pi Zero 2
const int I2cBusId = 3;
const int PinNumberPc5 = 13;
const int PinNumberPc6 = 11;
const int PinNumberPc9 = 7;

// 128 pixels wide, can fit ~20 characters depending on font and character size
const int MaxLcdDisplayWidth = 50;

// Wayback Machine constants
const string WaybackProxyUrl = "http://web.archive.org";
const string WaybackProxyString = "WaybackProxy";
const string WaybackProxyConfigFile = "/home/orangepi/src/WaybackProxy/config.json";
const string DefaultArchiveDate = "19970101";

int currentDialValue = 0;
// 0 = month, 1 = year
int dialMode = 0;

bool isProxyRunning = await PageContainsString(WaybackProxyUrl, WaybackProxyString);
if (isProxyRunning)
{
    Console.WriteLine("Internet Time Machine. Press Ctrl+C to end.");
}
else
{
    Console.WriteLine("Wayback Machine proxy is not running. Please start it first.");
    return;
}

DateTime startDate = await ReadWaybackProxyConfig(
    WaybackProxyConfigFile, DefaultArchiveDate);

// setup hardware - Orange Pi Zero 2
using GpioController gpioController = new GpioController(
    PinNumberingScheme.Board, new OrangePiZero2Driver());
// using Lcd1602 lcd = SetupLcd1602(I2cBusId);
using Ssd1306 lcd = SetupLcd1306(I2cBusId);
RefreshDisplay(lcd, startDate, dialMode, currentDialValue);

using ScaledQuadratureEncoder encoder = SetupRotaryEncoder(
    PinNumberPc6, PinNumberPc5,
    gpioController, currentDialValue);

// Register to Value change events
encoder.ValueChanged += (o, e) =>
{
    currentDialValue = (int)e.Value;
    Console.WriteLine($"Dial value: {currentDialValue}");
    RefreshDisplay(lcd, startDate, dialMode, currentDialValue);
};

GpioButton button = new GpioButton(PinNumberPc9, gpioController);
button.Press += (s, e) =>
{
    Console.WriteLine("Button pressed");
    DateTime newDate = GetNewDate(startDate, dialMode, currentDialValue);
    startDate = newDate;
    dialMode = (dialMode == 0) ? 1 : 0;
    encoder.Value = 0;
    RefreshDisplay(lcd, startDate, dialMode, currentDialValue);
};

// set initial archive date
await SetNewArchiveDate(startDate);
DateTime lastDate = startDate;

Console.WriteLine("Entering main loop");

// Main UI loop
while (true)
{
    DateTime newDate = GetNewDate(startDate, dialMode, currentDialValue);
    if (newDate != lastDate)
    {
        Console.WriteLine($"New archive date: {newDate}");
        await SetNewArchiveDate(newDate);
        lastDate = newDate;
    }

    Thread.Sleep(1000);
}

// a function to check if an url returns 200 response using HttpClient and HEAD request
static async Task<bool> PageContainsString(string url, string target)
{
    string responseBody = string.Empty;
    using var client = new HttpClient(GetHttpClientHandler());
    try
    {
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            // read response to string
            responseBody = await response.Content.ReadAsStringAsync();
        }

        return responseBody.Contains(target);
    }
    catch (Exception)
    {
        return false;
    }
}

// a function that makes GET request using HttpClient and returns the response as a string
static async Task SetNewArchiveDate(DateTime newDate)
{
    var client = new HttpClient(GetHttpClientHandler());
    string newDateString = newDate.ToString("yyyyMMdd");
    string url = $"{WaybackProxyUrl.TrimEnd('/')}/?date={newDateString}&dateTolerance=999&gcFix=on&quickImages=on&ctEncoding=on";
    var response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
}

static HttpClientHandler GetHttpClientHandler()
{
    HttpClientHandler httpClientHandler = new HttpClientHandler()
    {
        Proxy = new WebProxy
        {
            Address = new Uri("http://localhost:80"),
            BypassProxyOnLocal = false
        }
    };

    return httpClientHandler;
}

static async Task<DateTime> ReadWaybackProxyConfig(string WaybackProxyConfigFile, string DefaultArchiveDate)
{
    // config file used by Wayback Machine proxy
    var proxyConfig = JsonSerializer.Deserialize<WaybackProxyConfig>(
        await File.ReadAllTextAsync(WaybackProxyConfigFile));

    DateTime startDate;
    string dateString = proxyConfig?.Date ?? DefaultArchiveDate;
    if (dateString.Length == 8 &&
        DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime parsedDate))
    {
        startDate = parsedDate;
    }
    else
    {
        startDate = DateTime.ParseExact(DefaultArchiveDate, "yyyyMMdd", CultureInfo.InvariantCulture);
    }

    return startDate;
}

/*
static Lcd1602 SetupLcd1602(int I2cBusId)
{
    const int I2cBaseAddress = 0x27;

    Console.WriteLine("Setting up LCD");
    I2cDevice i2c = I2cDevice.Create(
        new I2cConnectionSettings(I2cBusId, I2cBaseAddress));
    Lcd1602 lcd = new Lcd1602(i2c, uses8Bit: false);
    Console.WriteLine("LCD initialized");
    return lcd;
}
*/

static Ssd1306 SetupLcd1306(int i2cBusId)
{
    const int I2cBaseAddress = 0x3C;

    Console.WriteLine("Setting up LCD");
    I2cDevice i2c = I2cDevice.Create(
        new I2cConnectionSettings(i2cBusId, I2cBaseAddress));
    Ssd1306 lcd = new Ssd1306(i2c);
    
    lcd.Initialize();
    Console.WriteLine("LCD initialized");
    return lcd;
}

static ScaledQuadratureEncoder SetupRotaryEncoder(
    int PinNumberPc6, int PinNumberPc5, GpioController gpioController, int currentDialValue)
{
    Console.WriteLine("Setting up rotary encoder");
    ScaledQuadratureEncoder encoder = new ScaledQuadratureEncoder(
        pinA: PinNumberPc5,
        pinB: PinNumberPc6,
        PinEventTypes.Falling,
        pulsesPerRotation: 30, pulseIncrement: 1.0,
        rangeMin: -200.0, rangeMax: 200.0, gpioController)
    {
        Value = currentDialValue,
        Debounce = TimeSpan.FromMilliseconds(100)
    };

    Console.WriteLine("Rotary encoder initialized");
    return encoder;
}

static DateTime GetNewDate(DateTime startDate, int dialMode, int currentDialValue)
{
    // the dial value seems to increase counter clockwise and decrease clockwise
    // so we need to invert it
    DateTime newDate;
    if (dialMode == 0)
    {
        newDate = startDate.AddMonths(currentDialValue * -1);
    }
    else
    {
        newDate = startDate.AddYears(currentDialValue * -1);
    }

    return newDate;
}

static void RefreshDisplay(Ssd1306 lcd, DateTime startDate, int dialMode, int currentDialValue)
{
    DateTime newDate = GetNewDate(startDate, dialMode, currentDialValue);

    lcd.Clear();
    lcd.SetCursorPosition(0, 0);
    lcd.Write("Web Time Machine");
    lcd.SetCursorPosition(0, 1);
    lcd.Write(newDate.ToString("Y").PadRight(MaxLcdDisplayWidth));
    lcd.SetCursorPosition(0, 2);
    string mode = (dialMode == 0) ? "Month" : "Year";
    lcd.Write($"Mode: {mode}");
}
