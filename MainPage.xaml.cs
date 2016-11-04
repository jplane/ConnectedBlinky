using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Windows.Devices.Gpio;
using Windows.UI.Xaml.Controls;
using Microsoft.Devices.Tpm;

namespace ConnectedBlinky
{
    public sealed partial class MainPage : Page
    {
        private Task _run;

        public MainPage()
        {
            this.InitializeComponent();

            Func<Task> runner = async () =>
            {
                var controller = await GpioController.GetDefaultAsync();

                var pin17 = controller.OpenPin(17);
                var pin18 = controller.OpenPin(18);

                pin17.SetDriveMode(GpioPinDriveMode.Output);
                pin18.SetDriveMode(GpioPinDriveMode.Output);

                var myDevice = new TpmDevice(0); // Use logical device 0 on the TPM by default
                var hubUri = myDevice.GetHostName();
                var deviceId = myDevice.GetDeviceId();
                var sasToken = myDevice.GetSASToken();

                var authMethod = AuthenticationMethodFactory.CreateAuthenticationWithToken(deviceId, sasToken);

                var deviceClient = DeviceClient.Create(hubUri, authMethod, TransportType.Amqp);

                while (true)
                {
                    var msg = await deviceClient.ReceiveAsync();

                    if (msg != null)
                    {
                        var body = Encoding.ASCII.GetString(msg.GetBytes());

                        await deviceClient.CompleteAsync(msg);

                        GpioPinValue redValue;
                        GpioPinValue greenValue;

                        switch (body.ToLowerInvariant())
                        {
                            case "red":
                                redValue = GpioPinValue.High;
                                greenValue = GpioPinValue.Low;
                                break;

                            case "green":
                                redValue = GpioPinValue.Low;
                                greenValue = GpioPinValue.High;
                                break;

                            default:
                                redValue = GpioPinValue.Low;
                                greenValue = GpioPinValue.Low;
                                break;
                        }

                        pin17.Write(redValue);
                        pin18.Write(greenValue);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            };

            _run = runner();
        }
    }
}
