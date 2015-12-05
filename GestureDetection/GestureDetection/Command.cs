using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureDetection
{
    public class Command
    {
        private CommandType commandType;
        private int volume;

        public int Volume
        {
            get { return volume; }
            set { volume = value; }
        }

        public CommandType CommandType
        {
            get { return commandType; }
            set { commandType = value; }
        }
    }

    public  enum CommandType
    {
        Play,
        Pause,
        Stop,
        Volume
    }
}
