using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using Microsoft.Win32;

namespace GestureDetection
{
    class VLCRemoteController
    {
        public bool isConnected = false;
        public VLCRemoteController()
        {
            m_client = new TcpClient();
        }

        ~VLCRemoteController()
        {
            disconnect();
        }

        public String getVLCExe()
        {
            String res = "";
            String keyName32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\VideoLAN\VLC";
            String keyName64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\VideoLAN\VLC";

            String valueName = "InstallDir";

            String location32 = (String)Registry.GetValue(keyName32, valueName, null);
            String location64 = (String)Registry.GetValue(keyName64, valueName, null);

            if (location32 != null)
            {
                String vlcexe32 = location32 + "\\vlc.exe";
                res = File.Exists(vlcexe32) ? vlcexe32 : "";
            }

            if (String.IsNullOrEmpty(res) && location64 != null)
            {
                String vlcexe64 = location64 + "\\vlc.exe";
                res = File.Exists(vlcexe64) ? vlcexe64 : "";
            }

            return res;
        }

        /**
         * Connect to VLC player
         * 
         * @return: true if success.
         * */
        public bool connect(String ip, Int32 port)
        {
            bool res = false;
            try
            {
                m_client = new TcpClient(ip, port);
                if (isSocketConnected())
                {
                    m_networkStream = m_client.GetStream();
                    isConnected = true;
                }
            }
            catch
            {
                Console.WriteLine("Error in VLCRemoteController::connect");
            }

            return res;
        }

        /**
         * Disconnect from VLC player.
         * */
        public void disconnect()
        {
            isConnected = false;
            if (isSocketConnected())
            {
                m_client.Close();
                m_networkStream.Close();
            }
        }

        /**
         * Send raw command to VLC. This function add \n to the end.
         * */
        public bool sendCustomCommand(String command)
        {
            bool res = false;
            if (isSocketConnected())
            {
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(command + "\n");
                m_networkStream.Write(data, 0, data.Length);
                res = true;
            }

            return res;
        }

        /**
         * Return current buffer from VLC player.
         * */
        public String reciveAnswer()
        {
            String res = String.Empty;
            if (isSocketConnected())
            {
                const int chunkSize = 4048 * 4048 * 4;
                Byte[] data = new Byte[chunkSize];

                //int readCount;
                //while ((readCount = ns.Read(data, 0, client.ReceiveBufferSize)) != 0)
                //{
                //    dataString.Append(Encoding.UTF8.GetString(data, 0, readCount));
                //}

                while (m_networkStream.DataAvailable)
                {
                    Int32 readBytes = m_networkStream.Read(data, 0, m_client.ReceiveBufferSize);
                    if (readBytes > 0)
                    {
                        res = res + System.Text.Encoding.UTF8.GetString(data, 0, readBytes);
                    }

                    if (!m_networkStream.DataAvailable)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }

            return res;
        }


        private bool isSocketConnected()
        {
            return m_client.Connected;
        }

        // This is client socket.
        private TcpClient m_client;

        // Stream for socket.
        private NetworkStream m_networkStream;
    }
}
