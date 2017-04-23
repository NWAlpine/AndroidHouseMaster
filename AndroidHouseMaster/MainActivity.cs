using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Net.Wifi;
using Android.Util;
using Android.Widget;
using Android.OS;

// debugging with android logging
// F5 app View -> Other Windows -> Android Device Logging
// https://developer.xamarin.com/guides/android/deployment,_testing,_and_metrics/android_debug_log/
// string tag = "myApp";
// Log.Info<Warn or Error> (tag, "this is a log entry);
//
// from command line
// nav to Android SDK i.e.C:\android-sdk-windows\platform-tools
// then execute $adb logcat

// tcp client example
// https://github.com/sethcall/async-helper/blob/master/src/AsyncTcpClient/AsyncTcpClient.cs

// Don't restart app on rotation, include in Activity
// ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize

namespace AndroidHouseMaster
{
    [Activity(Label = "AndroidHouseMaster",
        ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize,
        MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        /*static TcpClient client;*/
        /*Stream stm;*/

        AsyncTcpClient asyncTcpClient;

        Button buttonConnect;

        const string hostIp = "192.168.0.24"; // endpoint, 22 is the local machine, 24 is the garage controller
        const int port = 9000;

        Button btnRefresh;

        TextView txtKitchenDoor;
        TextView txtGarageDoor;
        TextView txtGarageLights;
        TextView txtGarageBayA;
        TextView txtGarageBayB;
        TextView txtGarageTemp;
        TextView txtFrontTemp;
        TextView txtFrontHumid;
        TextView txtFrontHI;

        const string celciusSymbol = "°C";
        const string fahernheightSymbol = "°F";
        bool isCelcius = true;  // default

        /*
        public bool IsConnected
        { get; set; }


        // event handler when data received needs to be processed and when disconnected
        public event EventHandler<byte[]> OnDataReceived;
        public event EventHandler OnDisconnected;
        */

        protected override void OnCreate(Bundle bundle)
        {
            // TODO: on rotation, this activity is executed again, need to handle this.
            // check if asyncTcpClient.IsConnected is true.
            // will need to move the connect logic up here first
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            buttonConnect = FindViewById<Button>(Resource.Id._buttonConnect);
            btnRefresh = FindViewById<Button>(Resource.Id._btnRefresh);

            #region UI text view

            txtKitchenDoor = FindViewById<TextView>(Resource.Id._txtKitchenDoor);
            txtGarageDoor = FindViewById<TextView>(Resource.Id._txtGarageDoor);
            txtGarageLights = FindViewById<TextView>(Resource.Id._txtGarageLights);
            txtGarageBayA = FindViewById<TextView>(Resource.Id._txtGarageBayA);
            txtGarageBayB = FindViewById<TextView>(Resource.Id._txtGarageBayB);
            txtGarageTemp = FindViewById<TextView>(Resource.Id._txtGarageTemp);
            txtFrontTemp = FindViewById<TextView>(Resource.Id._txtFrontTemp);
            txtFrontHumid = FindViewById<TextView>(Resource.Id._txtFrontHumidity);
            txtFrontHI = FindViewById<TextView>(Resource.Id._txtFrontHI);

            #endregion

            buttonConnect.Click += OnButtonConnectClick;

            // add the disconnect handler

            btnRefresh.Click += OnRefreshClick;

            asyncTcpClient = new AsyncTcpClient();
        }

        void OnButtonConnectClick(object o, EventArgs e)
        {
            #region working not referencing class
            //if (client == null)
            //{
            //    client = new TcpClient();
            //    client.Connect(hostIp, port);           // attempt connection
            //}

            //if (buttonConnect.Text == "Connect")
            //{
            //    // assign the event handler
            //    IsConnected = true;
            //    OnDataReceived += ProcessData;

            //    Toast.MakeText(this, "Running.", ToastLength.Short).Show();
            //    buttonConnect.Text = "Dis-Connect";

            //    // go!!
            //    Task receiveTask = ReceiveAsync();
            //}
            //else
            //{
            //    // we are disconnecting
            //}
            #endregion

            // first try and connect
            if (buttonConnect.Text == "Connect")
            {
                asyncTcpClient.OnDataReceived += ProcessData;
                asyncTcpClient.Connect(hostIp, port);

                Toast.MakeText(this, "Running.", ToastLength.Short).Show();

                buttonConnect.Text = "Dis-Connect";
                Task receiveTask = asyncTcpClient.ReceiveAsync();
            }
            else
            {
                // disconnect
                Task close = asyncTcpClient.CloseAsync();

                buttonConnect.Text = "Connect";

                Toast.MakeText(this, "Disconnected.", ToastLength.Short).Show();

                // clear out values
                ClearDataValues();
            }
        }

        void OnRefreshClick(object o, EventArgs e)
        {
            try
            {
                if (asyncTcpClient.IsConnected)
                {
                    // clear out the UI values
                    ClearDataValues();

                    // request a refresh of all values
                    //ClientSend("r0");
                    byte[] sendBytes = System.Text.Encoding.Unicode.GetBytes("r0");
                    //Task sendTask = SendAsync(sendBytes);
                    Task sendTask = asyncTcpClient.SendAsync(sendBytes);
                    
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, ex.Message, ToastLength.Short).Show();
            }
        }

        #region moved to class
        /*
        private async Task ReceiveAsync(CancellationToken token = default(CancellationToken))
        {
            byte[] dataSize = new byte[4];
            while (this.IsConnected)
            {
                token.ThrowIfCancellationRequested();
                stm = client.GetStream();
                int k = await stm.ReadAsync(dataSize, 0, sizeof(uint), token);  // 3rd entry in dataSize hold how many bytes to read

                byte[] data = new byte[dataSize[3]];
                int bytesRead = await stm.ReadAsync(data, 0, dataSize[3], token);

                var onDataReceived = this.OnDataReceived;
                if (OnDataReceived != null)
                {
                    onDataReceived(this, data);
                }
            }
        }

        private async Task SendAsync(byte[] data, CancellationToken token = default(CancellationToken))
        {
            try
            {
                stm = client.GetStream();
                await this.stm.WriteAsync(data, 0, data.Length, token);
                await this.stm.FlushAsync(token);
            }
            catch (IOException ex)
            {
                var onDisconnected = this.OnDisconnected;
                if (ex.InnerException != null && ex.InnerException is ObjectDisposedException)
                {
                    //Console.WriteLine("innocous SSL stream error);
                }
                else if (onDisconnected != null)
                {
                    onDisconnected(this, EventArgs.Empty);
                }
            }
        }
        */
        #endregion

        private void ProcessData(object o, byte[] sensorData)
        {
            string data = System.Text.Encoding.ASCII.GetString(sensorData);

            string sensorId = data.Substring(0, 1);
            string reading = data.Substring(1);
            int value;
            Int32.TryParse(reading, out value);

            string dispText = string.Empty;

            switch (sensorId)
            {
                case "g":
                    // Garage Door
                    dispText = string.Format("Garage Door {0}", DoorStateString(value));
                    DisplayData(txtGarageDoor, dispText);
                    break;

                case "k":
                    // Kitchen Door
                    dispText = string.Format("Kitchen Door {0}", DoorStateString(value));
                    DisplayData(txtKitchenDoor, dispText);
                    break;

                case "l":
                    // Garage Light
                    dispText = string.Format("Garage Light {0}", LightSateString(value));
                    DisplayData(txtGarageLights, dispText);
                    break;

                case "a":
                    // Bay A
                    dispText = string.Format("Garage Bay A {0}", BayStateString(value));
                    DisplayData(txtGarageBayA, dispText);
                    break;

                // missing Garage Bay B

                case "i":
                    // Garage Temp
                    dispText = string.Format("Garage Temp {0}{1}", value.ToString(), isCelcius ? celciusSymbol : fahernheightSymbol);
                    DisplayData(txtGarageTemp, dispText);
                    break;

                case "c":
                    // Front Temp
                    dispText = string.Format("Front Temp {0}{1}", value.ToString(), isCelcius ? celciusSymbol : fahernheightSymbol);
                    DisplayData(txtFrontTemp, dispText);
                    break;

                case "h":
                    // Front Humidity
                    dispText = string.Format("Front Humidity {0}{1}", value.ToString(), "%");
                    DisplayData(txtFrontHumid, dispText);
                    break;

                case "x":
                    // Front Heat Index
                    dispText = string.Format("Front Heat Index {0}{1}", value.ToString(), isCelcius ? celciusSymbol : fahernheightSymbol);
                    DisplayData(txtFrontHI, dispText);
                    break;

                default:
                    break;
            }
        }

        private void DisplayData(TextView view, string data)
        {
            this.RunOnUiThread(() =>
            {
                // RunOnUiThread is for updating UI not running methods
                view.Text = data;
            });
        }

        private string DoorStateString(int data)
        {
            string statusString = string.Empty;
            switch (data)
            {
                case 0:
                    statusString = "Closed";
                    break;

                case 1:
                    statusString = "Open";
                    break;

                case 2:
                    statusString = "Closing...";
                    break;

                case 3:
                    statusString = "Opening...";
                    break;

                default:
                    break;
            }

            return statusString;
        }

        private string LightSateString(int data)
        {
            string statusString = string.Empty;
            switch (data)
            {
                case 0:
                    statusString = "Off";
                    break;

                case 1:
                    statusString = "On";
                    break;

                default:
                    break;
            }

            return statusString;
        }

        private string BayStateString(int data)
        {
            string statusString = string.Empty;
            switch (data)
            {
                case 0:
                    statusString = "Occupied";
                    break;

                case 1:
                    statusString = "Vacant";
                    break;
            }

            return statusString;
        }

        private void ClearDataValues()
        {
            DisplayData(txtGarageDoor, "---");
            DisplayData(txtKitchenDoor, "---");
            DisplayData(txtGarageLights, "---");
            DisplayData(txtGarageBayA, "---");
            DisplayData(txtGarageTemp, "---");
            DisplayData(txtFrontTemp, "---");
            DisplayData(txtFrontHumid, "---");
            DisplayData(txtFrontHI, "---");
        }

    }
}

