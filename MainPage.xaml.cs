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

                var value = GpioPinValue.Low;

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
                        //var body = Encoding.ASCII.GetString(msg.GetBytes());

                        await deviceClient.CompleteAsync(msg);

                        pin17.Write(value);

                        value = value == GpioPinValue.Low ? GpioPinValue.High : GpioPinValue.Low;

                        pin18.Write(value);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            };

            _run = runner();
        }
    }
}
