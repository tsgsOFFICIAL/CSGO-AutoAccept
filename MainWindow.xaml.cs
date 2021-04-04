using System;
using System.Windows;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Tesseract;

namespace CS_GO_AutoAccept
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool runScanner = false;
        public static double tempxstartpos = 0;
        public static double tempystartpos = 0;

        #region Mouse stuff
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            MainScanner();
        }

        //EventHandlers

        /// <summary>
        /// State ON event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Program_state_Checked(object sender, RoutedEventArgs e)
        {
            //Change to a brighter color
            Program_state.Foreground = new SolidColorBrush(Colors.Green);

            Program_state.Content = "Auto Accept (ON)";
            runScanner = true;
        }

        /// <summary>
        /// State OFF event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Program_state_Unchecked(object sender, RoutedEventArgs e)
        {
            //Change to a darker color
            Program_state.Foreground = new SolidColorBrush(Colors.Red);

            Program_state.Content = "Auto Accept (OFF)";
            runScanner = false;
        }

        /// <summary>
        /// Discord button onClick event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Discord_Button_Click(object sender, RoutedEventArgs e)
        {
            //Launch browser to join Discord
            LaunchWeb(@"https://discord.gg/Cddu5aJ");
        }

        //Methods

        /// <summary>
        /// Launch a web URL on Windows, Linux and OSX
        /// </summary>
        /// <param name="url">The URL to open in the standard browser</param>
        public void LaunchWeb(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                //Hack for running the above line in DOT net Core...
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Main loop for running the scanner
        /// </summary>
        public static async void MainScanner()
        {
            ImageConverter converter = new ImageConverter();

            while (!runScanner)
            {
                await Task.Delay(500);

                while (runScanner)
                {
                    Bitmap region = CaptureMyScreen(220, 80, 850, 570); //Capture the accept button

                    using (TesseractEngine engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                    {
                        using (Pix img = Pix.LoadFromMemory((byte[])converter.ConvertTo(region, typeof(byte[]))))
                        {
                            using (Page page = engine.Process(img))
                            {
                                string text = page.GetText();
                                float confidence = page.GetMeanConfidence();

                                if (text.ToLower().Contains("accept") && confidence > 0.80) //Pretty sure its right
                                {
                                    System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)tempxstartpos + 10, (int)tempystartpos + 10);
                                    Thread.Sleep(100);
                                    uint X = (uint)System.Windows.Forms.Cursor.Position.X;
                                    uint Y = (uint)System.Windows.Forms.Cursor.Position.Y;
                                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0); //Left click
                                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0); //Left click twice for good measure
                                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0); //Left click 3 times to be 900% sure!

                                    Environment.Exit(0);
                                    runScanner = false;
                                }
                            }
                        }
                    }
                    await Task.Delay(500);
                    if (!runScanner)
                    {
                        //Stopped
                    }
                }
            }
        }

        /// <summary>
        /// Take a screen capture assuming the screen is in 1920 x 1080
        /// </summary>
        /// <param name="xheight">Height in pixels</param>
        /// <param name="xwidth">Width in pixels</param>
        /// <param name="xstartpos">X Starting position in pixels</param>
        /// <param name="ystartpos">Y Starting position in pixels</param>
        /// <returns>This method returns a bitmap of the area</returns>
        private static Bitmap CaptureMyScreen(int xheight, int xwidth, int xstartpos = 0, int ystartpos = 0)
        {
            // Convert original to any format
            double temp = (double)xheight / Screen.AllScreens[0].Bounds.Height * 100;
            double tempxheight = Screen.AllScreens[0].Bounds.Height * temp / 100;

            temp = (double)xwidth / Screen.AllScreens[0].Bounds.Width * 100;
            double tempxwidth = Screen.AllScreens[0].Bounds.Width * temp / 100;

            temp = (double)xstartpos / Screen.AllScreens[0].Bounds.Width * 100;
            tempxstartpos = Screen.AllScreens[0].Bounds.Width * temp / 100;

            temp = (double)ystartpos / Screen.AllScreens[0].Bounds.Height * 100;
            tempystartpos = Screen.AllScreens[0].Bounds.Height * temp / 100;

            try
            {
                //Creating a new Bitmap object

                Bitmap captureBitmap = new Bitmap((int)tempxheight, (int)tempxwidth, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                //Bitmap captureBitmap = new Bitmap(int width, int height, PixelFormat);  

                //Creating a Rectangle object which will capture our Current Screen

                Rectangle captureRectangle = Screen.AllScreens[0].Bounds;

                //Creating a New Graphics Object

                Graphics captureGraphics = Graphics.FromImage(captureBitmap);

                //Copying Image from The Screen

                if (xstartpos == 0 && ystartpos == 0)
                {
                    tempxstartpos = captureRectangle.Left;
                    tempystartpos = captureRectangle.Top;
                }

                captureGraphics.CopyFromScreen((int)tempxstartpos, (int)tempystartpos, 0, 0, captureRectangle.Size);

                //captureBitmap.Save(@"C:\Users\Marcus Jensen\Desktop\temp.png", System.Drawing.Imaging.ImageFormat.Png);

                return captureBitmap;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return null;
            }
        }
    }
}