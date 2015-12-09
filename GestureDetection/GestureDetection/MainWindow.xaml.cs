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
using LightBuzz.Vitruvius;
#endregion
namespace GestureDetection
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VLCRemoteController m_vlcControl;
        Timer timer = new Timer();
        Command prevCommand, command1, command2, command3;
        Joint Hip_right, Shoulder_right, Elbow_right, Hip_left, Shoulder_left, Elbow_left;
        int ref_angle_right, ref_angle_left;
        int framecount = 0;

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
            timer.Interval = 1000;
            //   timer.Elapsed += timer_Elapsed;//event handler for every seconds
            //to make the window start always in the bottom right corner
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;
            this.Topmost = true; //always listening.
            //    command = new Command();

            prevCommand = new Command();
            prevCommand.CommandType = CommandType.Play;

            command1 = new Command();
            command2 = new Command();
            command3 = new Command();


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
        void timer_Elapsed()
        {
            if (command1.CommandType == CommandType.Pause && command2.CommandType != CommandType.Pause && command3.CommandType != CommandType.Pause)
            {
                makeImageDisplay(3);
            }
            else if (command1.CommandType == CommandType.Pause && command2.CommandType == CommandType.Pause && command3.CommandType != CommandType.Pause)
            {
                makeImageDisplay(2);
            }
            else if (command1.CommandType == CommandType.Pause && command2.CommandType == CommandType.Pause && command3.CommandType == CommandType.Pause)
            {
                makeImageDisplay(1);
            }


        }
        Command getCommand()
        {
            if (command1.CommandType == command2.CommandType && command2.CommandType == command3.CommandType)
            {
                return command1;
            }
            else
            {
                return null;
            }

        }
        void resetCommands()
        {
            command1 = null;
            command2 = null;
            command3 = null;
        }
        /// <summary>
        /// Event Handler for the MultisourceFrameArrived event of the Kinect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            framecount++;
            if (framecount % 30 != 0)
                return;


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
                    TimeSpan span = frame.RelativeTime;
                    //  MessageBox.Show(span.Ticks.ToString());
                    //  if (span.Ticks % 60000 == 0) // i think it is relative seconds , 60000 msec is 1 sec. so for every second trying to run
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
                                    if (m_vlcControl.isConnected)
                                    {
                                        Hip_right = body.Joints[JointType.HipRight];
                                        Hip_left = body.Joints[JointType.HipLeft];

                                        Shoulder_right = body.Joints[JointType.ShoulderRight];
                                        Shoulder_left = body.Joints[JointType.ShoulderLeft];

                                        Elbow_right = body.Joints[JointType.ElbowRight];
                                        Elbow_left = body.Joints[JointType.ElbowLeft];

                                        ref_angle_right = (int)Math.Floor(Shoulder_right.Angle(Hip_right, Elbow_right));
                                        ref_angle_left = (int)Math.Floor(Shoulder_left.Angle(Hip_left, Elbow_left));

                                        Command command = null; // detect gesture and get the command
                                        icon.Source = null;
                                        Message.Content = "";
                                        //   Message.Content = "ref_angle :" + ref_angle;
                                        if (ref_angle_right > 70)
                                        {
                                            command = detectGestureAndCommand(body);
                                            // timer_Elapsed();
                                            command3 = command2; // moving command 2 to command 3
                                            command2 = command1; // moving command 1 to command 2;
                                            command1 = command;
                                            //  command = currentcommand;
                                            command = getCommand();
                                            if (command != null)
                                            {
                                                if (command.CommandType == CommandType.Play)
                                                {
                                                    if (prevCommand.CommandType != CommandType.Play)
                                                    {

                                                        issuePlayCommand(); //issue play command
                                                        prevCommand = command;
                                                    }

                                                }

                                                else if (command.CommandType == CommandType.Pause)
                                                {
                                                    if (prevCommand.CommandType != CommandType.Pause)
                                                    {
                                                        issuePauseCommand(); //issue pause command
                                                        prevCommand = command;
                                                    }

                                                }
                                                else if (command.CommandType == CommandType.Stop)
                                                {
                                                    if (prevCommand.CommandType != CommandType.Stop)
                                                    {
                                                        issueStopCommand(); //issue stop comand
                                                        prevCommand = command;
                                                    }

                                                }

                                               // resetCommands();
                                            }


                                        }
                                        else
                                        {
                                            // playCommandCount = 0; pauseCommandCount = 0; stopCommandCount = 0;
                                        }
                                        if (ref_angle_left > 70)
                                        {
                                            command = detectGestureAndCommand(body);
                                            /*command3 = command2; // moving command 2 to command 3
                                            command2 = command1; // moving command 1 to command 2;
                                            command1 = command;
                                            command = getCommand();*/
                                            if (command != null)
                                            {
                                                if (command.CommandType == CommandType.Volume /*&& prevCommand.CommandType == CommandType.Play*/)
                                                    issueVolumeCommand(command.Volume + ""); //issue volume command
                                            }
                                        }



                                    }
                                }
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

            lock (this)
            {
                if (m_vlcControl.sendCustomCommand("play"))
                {
                    if (prevCommand.CommandType != CommandType.Stop)
                    {
                        m_vlcControl.sendCustomCommand("pause");
                        icon.Source = new BitmapImage(new Uri("pack://application:,,,/play106.png"));
                        Message.Content = "Command: Play";
                    }
                    else
                    {
                        icon.Source = new BitmapImage(new Uri("pack://application:,,,/play106.png"));
                        Message.Content = "Command: Play";
                    }
                }
                else
                {
                    icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                    Message.Content = "Play Command Issue Failure";
                }
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
                //   timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Pause Command Issue Failure";
                //   timer.Start();
            }
        }
        /// <summary>
        /// to issue stop command to the VLC Media Player through RC Interface
        /// </summary>
        private void issueStopCommand()
        {
            //issue pause command

            if(prevCommand.CommandType == CommandType.Pause)
            {
                m_vlcControl.sendCustomCommand("play");
                m_vlcControl.sendCustomCommand("pause");
                m_vlcControl.sendCustomCommand("stop");
            }
            else
            {
                 m_vlcControl.sendCustomCommand("stop");
            }
            icon.Source = new BitmapImage(new Uri("pack://application:,,,/media26.png"));
                Message.Content = "Command: Stop";



          /*  if (m_vlcControl.sendCustomCommand("stop"))
            {
                //need to say the command in UI
                if (prevCommand.CommandType == CommandType.Pause)
                {
                    m_vlc
                }
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/media26.png"));
                Message.Content = "Command: Stop";
                //   timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Stop Command Issue Failure";
                //  timer.Start();
            }*/
        }
        private void issueVolumeCommand(String volume)
        {
            string command_string = "volume " + volume;
            if (m_vlcControl.sendCustomCommand(command_string))
            {
                m_vlcControl.reciveAnswer();
                //need to say the command in UI
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/media26.png"));
                Message.Content = "Command: Volume " + volume;
              //  Message.Content = m_vlcControl.reciveAnswer(); 
                //   timer.Start();
            }
            else
            {
                icon.Source = new BitmapImage(new Uri("pack://application:,,,/cross97.png"));
                Message.Content = "Volume Command Issue Failure";
                //  timer.Start();
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
            else if (isVolumeCommand(body))
            {
                int volume = getVolumeFromAngle(body);
                command.CommandType = CommandType.Volume;
                command.Volume = volume;
            }



            return command;
        }
        private int getVolumeFromAngle(Body body)
        {
            int volume = 0;
            Joint shoulder_left = body.Joints[JointType.ShoulderLeft];
            Joint elbow_left = body.Joints[JointType.ElbowLeft];
            Joint wrist_left = body.Joints[JointType.WristLeft];
            int Angle = (int)Math.Floor(elbow_left.Angle(shoulder_left, wrist_left));
            Message.Content = Angle;
            if (Angle >= 160)
                volume = 400;
            else if (Angle <= 60)
                volume = 1;
            else
            {
                if (Angle > 60 && Angle <= 90)
                    volume = 100;
                else if (Angle > 90 && Angle <= 120)
                    volume = 200;
                else if (Angle > 120 && Angle <= 150)
                    volume = 300;
                else
                    volume = 400;
            }
           // Message.Content = "Volume :" + volume;
            return volume;
        }
        private bool isVolumeCommand(Body body)
        {
            bool isVolume = false;
            if (body.HandLeftState == HandState.Lasso)
            {
               
                    isVolume = true;
            }
            //  isVolume = true;
            return isVolume;
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
            {

                isPlay = true;

            }

            return isPlay;
        }

        private void makeImageDisplay(int count)
        {
            Counter.Visibility = System.Windows.Visibility.Visible;
            Message.Content = count;
            switch (count)
            {
                case 1: Counter.Source = new BitmapImage(new Uri("pack://application:,,,/1.png"));
                    break;
                case 2: Counter.Source = new BitmapImage(new Uri("pack://application:,,,/2.png"));
                    break;
                case 3: Counter.Source = new BitmapImage(new Uri("pack://application:,,,/3.png"));
                    break;
            }

        }
        private void makeImageHidden()
        {
            Counter.Visibility = System.Windows.Visibility.Hidden;
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
            {


                isStop = true;


            }

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
            {

                isPause = true;
            }
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
                m_vlcControl.connect("127.0.0.1", 4444);
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
            /* if (m_vlcControl.connect("127.0.0.1", 4444))
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
             */
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
