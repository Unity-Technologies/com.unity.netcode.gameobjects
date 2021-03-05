using System;
using System.Net.Sockets;

namespace MLAPI.Transports.Tasks
{
    /// <summary>
    /// Represents one or more socket tasks.
    /// </summary>
    public class SocketTasks
    {
        /// <summary>
        /// Gets or sets the underlying SocketTasks.
        /// </summary>
        /// <value>The tasks.</value>
        public SocketTask[] Tasks { get; set; }

        /// <summary>
        /// Gets a value indicating whether this all tasks is done.
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets a value indicating whether all tasks were sucessful.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets a value indicating whether any tasks were successful.
        /// </summary>
        /// <value><c>true</c> if any success; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets a value indicating whether any tasks are done.
        /// </summary>
        /// <value><c>true</c> if any done; otherwise, <c>false</c>.</value>
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

    /// <summary>
    /// A single socket task.
    /// </summary>
    public class SocketTask
    {
        // Used for states
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MLAPI.Transports.Tasks.SocketTask"/> is done.
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
        public bool IsDone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MLAPI.Transports.Tasks.SocketTask"/> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        // These are all set by the transport
        /// <summary>
        /// Gets or sets the transport exception.
        /// </summary>
        /// <value>The transport exception.</value>
        public Exception TransportException { get; set; }

        /// <summary>
        /// Gets or sets the socket error.
        /// </summary>
        /// <value>The socket error.</value>
        public SocketError SocketError { get; set; }

        /// <summary>
        /// Gets or sets the transport code.
        /// </summary>
        /// <value>The transport code.</value>
        public int TransportCode { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        public object State { get; set; }

        /// <summary>
        /// Gets a done task.
        /// </summary>
        /// <value>The done.</value>
        public static SocketTask Done => new SocketTask
        {
            IsDone = true,
            Message = null,
            SocketError = SocketError.Success,
            State = null,
            Success = true,
            TransportCode = -1,
            TransportException = null
        };

        /// <summary>
        /// Gets a faulty task.
        /// </summary>
        /// <value>The fault.</value>
        public static SocketTask Fault => new SocketTask
        {
            IsDone = true,
            Message = null,
            SocketError = SocketError.SocketError,
            State = null,
            Success = false,
            TransportCode = -1,
            TransportException = null
        };

        /// <summary>
        /// Gets a working task.
        /// </summary>
        /// <value>The working.</value>
        public static SocketTask Working => new SocketTask
        {
            IsDone = false,
            Message = null,
            SocketError = SocketError.SocketError,
            State = null,
            Success = false,
            TransportCode = -1,
            TransportException = null
        };

        /// <summary>
        /// Converts to a SocketTasks.
        /// </summary>
        /// <returns>The tasks.</returns>
        public SocketTasks AsTasks() => new SocketTasks { Tasks = new SocketTask[] { this } };
    }
}