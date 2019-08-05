using System;
using System.Net.Sockets;

namespace MLAPI.Transports.Tasks
{
    public class SocketTasks
    {
        public SocketTask[] Tasks { get; set; }

        public bool IsDone 
        {
            get
            {
                for (int i = 0; i < Tasks.Length; i++)
                {
                    if (!Tasks[i].IsDone)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool Success
        {
            get
            {
                for (int i = 0; i < Tasks.Length; i++)
                {
                    if (!Tasks[i].Success)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool AnySuccess
        {
            get
            {
                for (int i = 0; i < Tasks.Length; i++)
                {
                    if (Tasks[i].Success)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool AnyDone
        {
            get
            {
                for (int i = 0; i < Tasks.Length; i++)
                {
                    if (Tasks[i].IsDone)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }

    public class SocketTask
    {
        // Used for states
        public bool IsDone { get; set; }
        public bool Success { get; set; }

        // These are all set by the transport
        public Exception TransportException { get; set; }
        public SocketError SocketError { get; set; }
        public int TransportCode { get; set; }
        public string Message { get; set; }
        public object State { get; set; }

        public static SocketTask Done => new SocketTask()
        {
            IsDone = true,
            Message = null,
            SocketError = SocketError.Success,
            State = null,
            Success = true,
            TransportCode = -1,
            TransportException = null
        };

        public static SocketTask Fault => new SocketTask()
        {
            IsDone = true,
            Message = null,
            SocketError = SocketError.SocketError,
            State = null,
            Success = false,
            TransportCode = -1,
            TransportException = null
        };

        public static SocketTask Working => new SocketTask()
        {
            IsDone = false,
            Message = null,
            SocketError = SocketError.SocketError,
            State = null,
            Success = false,
            TransportCode = -1,
            TransportException = null
        };

        public SocketTasks AsTasks()
        {
            return new SocketTasks()
            {
                Tasks = new SocketTask[] { this }
            };
        }
    }
}
