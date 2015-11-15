using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#region Default Imports
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Diagnostics;
using System.Timers;
#endregion
#region Kinect Specific Imports
using Microsoft.Kinect;
#endregion
namespace GestureDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VLCRemoteController m_vlcControl;
        Timer timer = new Timer(3000);
        #region Members
        Mode _mode = Mode.Color;
        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            m_vlcControl = new VLCRemoteController(); //initialize

        }
        /// <summary>
        /// Form Loaded Method for the WPF Form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //to make the window start always in the bottom left corner
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
            this.Topmost = true; //always listening.
           


            timer.Elapsed += timer_Elapsed;//event handler for every 3 seconds
            timer.Start();
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body); // reading the body and color sensor
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived; // whenever a multi-source frame arrives handle this event
                gestureIcon.Source = new BitmapImage(new Uri("pack://application:,,,/gesture-icon.png"));
                gestureMessage.Content = "Gesture Detection: On";
            }
        }
        void clearMessages()
        {
            //every 3 seconds resetting the icon and message
            icon.Source = null;
            Message.Content = "";
        }
        void timer_Elapsed(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(clearMessages);
            timer.Stop();

        }
        /// <summary>
        /// Event Handler for the MultisourceFrameArrived event of the Kinect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();
            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        camera.Source = frame.ToBitmap(); // to set the image in the Camera Ciew
                    }
                }
            }
            //Body to get the joints and detect Gesture
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();
                    _bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(_bodies);
                    foreach (var body in _bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                Command command = detectGestureAndCommand(body); // detect gesture and get the command
                                if (command.CommandType == CommandType.Play)
                                    issuePlayCommand(); //issue play command
                                else if (command.CommandType == CommandType.Pause)
                                    issuePauseCommand(); //issue pause command
                                else if (command.CommandType == CommandType.Stop)
                                    issueStopCommand(); //issue stop comand

                            }
                        }
                    }
                }
            }

        }
        /// <summary>
        /// to issue play command to the VLC Media Player through RC Interface
        /// </summary>
        private void issuePlayCommand() //issue play command to VLC Media Player
        {
            //to issue play command to vlc remote interface we need to issue play command and 
            //after we receive reply we need to send pause again to issue actual Play command
            if (m_vlcControl.sendCustomCommand("play"))
            {
                m_vlcControl.reciveAnswer();
                if (m_vlcControl.sendCustomCommand("pause"))
                {
                    //need to say the command in UI
                    icon.Source = new BitmapImage(new Uri("pack://application:,,,/play106.png"));
                    Message.Content = "Command: Play";
                    timer.Start();
                }

            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Play Command Issue Failure";
                timer.Start();
            }
        }
        /// <summary>
        /// //issue pause command to VLC Media Player through RC Interface
        /// </summary>
        private void issuePauseCommand()
        {
            //issue pause command
            if (m_vlcControl.sendCustomCommand("pause"))
            {
                //need to say the command in UI
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/pause44.png"));
                Message.Content = "Command: Pause";
                timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Pause Command Issue Failure";
                timer.Start();
            }
        }
        /// <summary>
        /// to issue stop command to the VLC Media Player through RC Interface
        /// </summary>
        private void issueStopCommand()
        {
            //issue pause command
            if (m_vlcControl.sendCustomCommand("stop"))
            {
                //need to say the command in UI
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/media26.png"));
                Message.Content = "Command: Stop";
                timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Stop Command Issue Failure";
                timer.Start();
            }
        }
        /// <summary>
        /// to detect the gesture and get the command
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        private Command detectGestureAndCommand(Body body)
        {
            Command command = new Command();
            if (isPlayGesture(body)) // check if its a play Command
                command.CommandType = CommandType.Play;
            else if (isPauseGesture(body)) //check if its a pause command
                command.CommandType = CommandType.Pause;
            else if (isStopGesture(body)) // check if its a stop command
                command.CommandType = CommandType.Stop;



            return command;
        }

        /// <summary>
        /// check whether a play gesture
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        private bool isPlayGesture(Body body)
        {
            bool isPlay = false;
            if (body.HandRightState == HandState.Open)
                isPlay = true;
            return isPlay;
        }

        /// <summary>
        ///check whether a stop gesture
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        private bool isStopGesture(Body body)
        {
            bool isStop = false;
            if (body.HandRightState == HandState.Closed)
                isStop = true;
            return isStop;
        }
        /// <summary>
        /// check whether a pause gesture
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>

        private bool isPauseGesture(Body body)
        {
            bool isPause = false;
            if (body.HandRightState == HandState.Lasso)
                isPause = true;
            return isPause;
        }
        /// <summary>
        /// on close of the wpf form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Close_Click(object sender, RoutedEventArgs e)
        {
           // timer.Stop();
            timer.Dispose();// dispose the timer
            m_vlcControl.disconnect(); // disconnect from VLC
            Application.Current.Shutdown();//close the application
        }
        /// <summary>
        /// to run vlc with the remote control interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_StartVLC_Click(object sender, RoutedEventArgs e)
        {
            String exePath = m_vlcControl.getVLCExe();
            if (!String.IsNullOrEmpty(exePath))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exePath;
                startInfo.Arguments = @"--control=rc --rc-host 127.0.0.1:4444";
                Process.Start(startInfo);
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/check35.png"));
                Message.Content = "Connection: Successful";
                timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Connection: Failure";
                timer.Start();
            }
        }
        /// <summary>
        /// to connect to the VLC remote interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ConnectVLC_Click(object sender, RoutedEventArgs e)
        {
            if (m_vlcControl.connect("127.0.0.1", 4444))
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/check35.png"));
                Message.Content = "Connection: Successful";
                timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Connection: Failure";
                timer.Start();
            }

        }
        /// <summary>
        /// to disconnect from the vlc remote interface
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_DisconnectVLC_Click(object sender, RoutedEventArgs e)
        {
            m_vlcControl.disconnect();
            icon.Source = new BitmapImage(new Uri("pack://application:,,,/check35.png"));
            Message.Content = "Disconnected Successfully";
            timer.Start();
        }
        /// <summary>
        /// to implement drag and drop feature for the form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }       

    }
    public enum Mode
    {
        Color
    }
}
