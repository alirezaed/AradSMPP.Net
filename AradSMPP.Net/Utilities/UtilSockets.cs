#region Namespaces
using System.Net;
using System.Net.Sockets;
using System.Text;
#endregion

namespace AradSMPP.Net.Utilities;

#region Class - SocketClient

/// <summary> This class abstract a socket </summary>
public class SocketClient : IDisposable
{
    #region Delegate Function Types

    /// <summary> Called when a message is extracted from the socket </summary>
    public delegate void MessageHandler(SocketClient socket);

    /// <summary> Called when a socket connection is closed </summary>
    public delegate void CloseHandler(SocketClient socket);

    /// <summary> Called when a socket error occurs </summary>
    public delegate void ErrorHandler(SocketClient socket, Exception exception);

    #endregion

    #region Static Properties

    /// <summary> Maintain the next unique key </summary>
    private static long _nextUniqueKey;

    #endregion

    #region Private Properties

    /// <summary> Flag when disposed is called </summary>
    private bool _disposed;
        
    /// <summary> The SocketServer for this socket object </summary>
    private readonly SocketServer _socketServer;

    /// <summary> The socket for the accepted client connection </summary>
    private Socket _clientSocket;

    /// <summary> A TcpClient object for client established connections </summary>
    private TcpClient _tcpClient;

    /// <summary> A network stream object </summary>
    private NetworkStream _networkStream;

    /// <summary> RetType: A callback object for processing recieved socket data </summary>	    
    private readonly AsyncCallback _callbackReadFunction;

    /// <summary> RetType: A callback object for processing send socket data </summary>
    private readonly AsyncCallback _callbackWriteFunction;

    /// <summary> A reference to a user supplied function to be called when a socket message arrives </summary>
    private readonly MessageHandler _messageHandler;

    /// <summary> A reference to a user supplied function to be called when a socket connection is closed </summary>
    private readonly CloseHandler _closeHandler;

    /// <summary> A reference to a user supplied function to be called when a socket error occurs </summary>
    private readonly ErrorHandler _errorHandler;

    #endregion

    #region Public Properties

    /// <summary> The IpAddress of the connection </summary>
    public string? IpAddress;

    /// <summary> The Port of the connection </summary>summary>
    public int Port = int.MinValue;

    /// <summary> The index position in the server dictionary of socket connections </summary>
    public int SocketIndex = -1;

    /// <summary> A raw buffer to capture data comming off the socket </summary>
    public byte[] RawBuffer;

    /// <summary> Size of the raw buffer for received socket data </summary>
    public int SizeOfRawBuffer;

    /// <summary> The length of the message </summary>
    public int MessageLength;

    /// <summary> A unique key for the socket object </summary>
    public long UniqueKey;

    /// <summary> A flag to determine if the Socket Client is connected </summary>
    public bool IsConnected => (true) ? _clientSocket.Connected : (true) && _tcpClient.Connected;
    #endregion

    #region User Defined Public Properties

    /// <summary> A string buffer to be used by the application developer </summary>
    public StringBuilder StringBuffer;

    /// <summary> A memory stream buffer to be used by the application developer </summary>
    public MemoryStream MessageBuffer;

    /// <summary> A byte buffer to be used by the application developer </summary>
    public byte[] ByteBuffer;

    /// <summary> A list buffer to be used by the application developer </summary>
    public List<byte> ListBuffer;

    /// <summary> The number of bytes that have been buffered </summary>
    public int BufferedBytes;

    /// <summary> A reference to a user defined object to be passed through the handler functions </summary>
    public object UserArg;

    /// <summary> UserDefined flag to indicate if the socket object is available for use </summary>
    public bool IsAvailable;

    #endregion

    #region Constructor

    /// <summary> Constructor for client support </summary>
    /// <param name="sizeOfRawBuffer"> The size of the raw buffer </param>
    /// <param name="sizeOfByteBuffer"> The size of the byte buffer </param>
    /// <param name="userArg"> A Reference to the Users arguments </param>
    /// <param name="messageHandler"> Reference to the user defined message handler function </param>
    /// <param name="closeHandler"> Reference to the user defined close handler function </param>
    /// <param name="errorHandler"> Reference to the user defined error handler function </param>
    public SocketClient(int sizeOfRawBuffer, int sizeOfByteBuffer, object userArg,
                        MessageHandler messageHandler, CloseHandler closeHandler, ErrorHandler errorHandler)
    {
        // Create the raw buffer
        SizeOfRawBuffer = sizeOfRawBuffer;
        RawBuffer = new byte[SizeOfRawBuffer];

        // Save the user argument
        UserArg = userArg;

        // Allocate a String Builder class for Application developer use
        StringBuffer = new();

        // Allocate a Memory Stream class for Application developer use
        MessageBuffer = new();

        // Allocate a byte buffer for Application developer use
        ByteBuffer = new byte[sizeOfByteBuffer];
        BufferedBytes = 0;

        // Allocate a list buffer for Application developer use
        ListBuffer = [];

        // Set the handler functions
        _messageHandler = messageHandler;
        _closeHandler = closeHandler;
        _errorHandler = errorHandler;

        // Set the async socket function handlers
        _callbackReadFunction = ReceiveComplete;
        _callbackWriteFunction = SendComplete;

        // Set available flags
        IsAvailable = true;

        // Set the unique key for this object
        UniqueKey = NewUniqueKey();
    }

    /// <summary> Constructor for SocketServer Suppport </summary>
    /// <param name="socketServer"> A Reference to the parent SocketServer </param>
    /// <param name="clientSocket"> The Socket object we are encapsulating </param>
    /// <param name="ipAddress"> The IpAddress of the remote server </param>
    /// <param name="port"> The Port of the remote server </param>
    /// <param name="sizeOfRawBuffer"> The size of the raw buffer </param>
    /// <param name="sizeOfByteBuffer"> The size of the byte buffer </param>
    /// <param name="userArg"> A Reference to the Users arguments </param>
    /// <param name="messageHandler"> Reference to the user defined message handler function </param>
    /// <param name="closeHandler"> Reference to the user defined close handler function </param>
    /// <param name="errorHandler"> Reference to the user defined error handler function </param>
    public SocketClient(SocketServer socketServer, Socket clientSocket, string? ipAddress, int port,
                        int sizeOfRawBuffer, int sizeOfByteBuffer, object userArg,
                        MessageHandler messageHandler, CloseHandler closeHandler, ErrorHandler errorHandler)
    {
        // Set reference to SocketServer
        _socketServer = socketServer;

        // Set when this socket came from a SocketServer Accept
        _clientSocket = clientSocket;
            
        // Set the Ipaddress and Port
        IpAddress = ipAddress;
        Port = port;

        // Set the server index
        SocketIndex = clientSocket.Handle.ToInt32();

        // Set the handler functions
        _messageHandler = messageHandler;
        _closeHandler = closeHandler;
        _errorHandler = errorHandler;

        // Create the raw buffer
        SizeOfRawBuffer = sizeOfRawBuffer;
        RawBuffer = new byte[SizeOfRawBuffer];

        // Save the user argument
        UserArg = userArg;

        // Allocate a String Builder class for Application developer use
        StringBuffer = new();

        // Allocate a Memory Stream class for Application developer use
        MessageBuffer = new();

        // Allocate a byte buffer for Application developer use
        ByteBuffer = new byte[sizeOfByteBuffer];
        BufferedBytes = 0;

        // Allocate a list buffer for Application developer use
        ListBuffer = [];

        // Init the NetworkStream reference
        _networkStream = new(_clientSocket);

        // Set the async socket function handlers
        _callbackReadFunction = ReceiveComplete;
        _callbackWriteFunction = SendComplete;

        // Set Available flags
        IsAvailable = true;

        // Set the unique key for this object
        UniqueKey = NewUniqueKey();

        // Set these socket options
        _clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReceiveBuffer, sizeOfRawBuffer);
        _clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.SendBuffer, sizeOfRawBuffer);
        _clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.KeepAlive, 1);
        _clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.DontLinger, 1);
        _clientSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Tcp, System.Net.Sockets.SocketOptionName.NoDelay, 1);
    }

    /// <summary> Dispose </summary>
    public void Dispose()
    {
        try
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        catch
        {
        }
    }
        
    /// <summary> Dispose the server </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        // Check to see if Dispose has already been called.
        if (!_disposed)
        {
            // Note disposing has been done.
            _disposed = true;

            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                try
                {
                    Disconnect();
                }

                catch
                {
                }
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary> Called when a message arrives </summary>
    /// <param name="ar"> An async result interface </param>
    private void ReceiveComplete(IAsyncResult ar)
    {
        try
        {
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "NetThreadPool";
            }

            // Is the Network Stream object valid
            if ((_networkStream != null) && (_networkStream.CanRead))
            {
                // Read the current bytes from the stream buffer
                MessageLength = _networkStream.EndRead(ar);

                // If there are bytes to process else the connection is lost
                if (MessageLength > 0)
                {
                    try
                    {
                        // A message came in send it to the MessageHandler
                        _messageHandler(this);
                    }

                    catch
                    {
                    }

                    // Wait for a new message
                    Receive();
                }
                else
                {
                    if (_networkStream != null)
                    {
                        Disconnect();
                    }
                        
                    // Call the close handler
                    _closeHandler(this);
                }
            }
            else
            {
                if (_networkStream != null)
                {
                    Disconnect();
                }
                    
                // Call the close handler
                _closeHandler(this);
            }
        }

        catch (Exception exception)
        {
            if (_networkStream != null)
            {
                Disconnect();
                    
                if ((!exception.Message.Contains("forcibly closed")) &&
                    (!exception.Message.Contains("thread exit")))
                {
                    _errorHandler(this, exception);
                }
            }
                
            // Call the close handler
            _closeHandler(this);
        }

        ar.AsyncWaitHandle.Close();
    }

    /// <summary> Called when a message is sent </summary>
    /// <param name="ar"> An async result interface </param>
    private void SendComplete(IAsyncResult ar)
    {
        try
        {
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = "NetThreadPool";
            }

            // Is the Network Stream object valid
            if ((_networkStream != null) && (_networkStream.CanWrite))
            {
                _networkStream.EndWrite(ar);
            }
        }

        catch
        {
        }

        ar.AsyncWaitHandle.Close();
    }

    #endregion

    #region Public Methods

    /// <summary> Called to generate a unique key </summary>
    /// <returns> long </returns>
    public static long NewUniqueKey()
    {
        // Set the unique key for this object
        return Interlocked.Increment(ref SocketClient._nextUniqueKey);
    }

    /// <summary> Function used to connect to a server </summary>
    /// <param name="ipAddress"> The address to connect to </param>
    /// <param name="port"> The Port to connect to </param>
    public void Connect(string? ipAddress, int port)
    {
        // If this object was disposed and they are trying to re-connect clear the flag
        if (_disposed)
        {
            throw new("ClientSocket Has Been Disposed");
        }

        if (_networkStream == null)
        {
            // Set the Ipaddress and Port
            IpAddress = ipAddress;
            Port = port;
                    
            try
            {
                IPAddress useIpAddress = null;
                IPHostEntry hostEntries = Dns.GetHostEntry(IpAddress);
                foreach (IPAddress address in hostEntries.AddressList)
                {
                    // Find the IPv4 address first
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        useIpAddress = address;
                        break;
                    }
                }
                        
                // Now just use the first address
                if (useIpAddress == null)
                {
                    useIpAddress = hostEntries.AddressList[0];
                }
                    
                IpAddress = useIpAddress.ToString();
            }

            catch
            {
                IpAddress = ipAddress;
            }
                    
            // Attempt to establish a connection
            _tcpClient = new(IpAddress, Port);
            _networkStream = _tcpClient.GetStream();

            // Set these socket options
            _tcpClient.ReceiveBufferSize = SizeOfRawBuffer;
            _tcpClient.SendBufferSize = SizeOfRawBuffer;
            _tcpClient.NoDelay = true;
            _tcpClient.LingerState = new(false, 0);

            // Start to receive messages
            Receive();
        }
    }

    /// <summary> Called to disconnect the client </summary>
    public void Disconnect()
    {
        try
        {
            // Remove the socket from the list
            if (_socketServer != null)
            {
                _socketServer.RemoveSocket(this);
            }

            // Set when this socket came from a SocketServer Accept
            if (_clientSocket != null)
            {
                _clientSocket.Close();
            }

            // Set when this socket came from a SocketClient Connect
            if (_tcpClient != null)
            {
                _tcpClient.Close();
            }
                   
            // Set it both cases
            if (_networkStream != null)
            {
                _networkStream.Close();
            }

            // Clean up the connection state
            _clientSocket = null;
            _tcpClient = null;
            _networkStream = null;
        }

        catch (Exception exception)
        {
            _errorHandler(this, exception);
        }
    }

    /// <summary> Function to send a string to the server </summary>
    /// <param name="message"> A string to send </param>
    public void Send(string message)
    {
        try
        {
            if ((_networkStream != null) && (_networkStream.CanWrite))
            {
                // Convert the string into a Raw Buffer
                byte[] pRawBuffer = System.Text.Encoding.ASCII.GetBytes(message);

                // Issue an asynchronus write
                _networkStream.BeginWrite(pRawBuffer, 0, pRawBuffer.Length, _callbackWriteFunction, null);
            }
            else
            {
                throw new("No Connection");
            }
        }
            
        catch
        {
            Disconnect();
                
            throw;
        }
    }

    /// <summary> Function to send a raw buffer to the server </summary>
    /// <param name="rawBuffer"> A Raw buffer of bytes to send </param>
    public void Send(byte[] rawBuffer)
    {
        try
        {
            if ((_networkStream != null) && (_networkStream.CanWrite))
            {
                // Issue an asynchronus write
                _networkStream.BeginWrite(rawBuffer, 0, rawBuffer.Length, _callbackWriteFunction, null);
            }
            else
            {
                throw new("No Connection");
            }
        }
            
        catch
        {
            Disconnect();
                
            throw;
        }
    }

    /// <summary> Function to send a char to the server </summary>
    /// <param name="charValue"> A Raw char to send </param>
    public void Send(char charValue)
    {
        try
        {
            if ((_networkStream != null) && (_networkStream.CanWrite))
            {
                // Convert the character to a byte
                byte[] pRawBuffer = [Convert.ToByte(charValue)];

                // Issue an asynchronus write
                _networkStream.BeginWrite(pRawBuffer, 0, pRawBuffer.Length, _callbackWriteFunction, null);
            }
            else
            {
                throw new("No Connection");
            }
        }
            
        catch
        {
            Disconnect();
                
            throw;
        }
    }

    /// <summary> Wait for a message to arrive </summary>
    public void Receive()
    {
        if ((_networkStream != null) && (_networkStream.CanRead))
        {
            // Issue an asynchronous read
            _networkStream.BeginRead(RawBuffer, 0, SizeOfRawBuffer, _callbackReadFunction, null);
        }
        else
        {
            throw new("Unable To Read From Stream");
        }
    }

    #endregion
}

#endregion

#region Class - SocketServer

/// <summary> This class accepts multiple socket connections and handles them asychronously </summary>
public class SocketServer : IDisposable
{
    #region Delagate Function Types

    /// <summary> Called when a message is extracted from the socket </summary>
    public delegate void MessageHandler(SocketClient socket);

    /// <summary> Called when a socket connection is closed </summary>
    public delegate void CloseHandler(SocketClient socket);

    /// <summary> Called when a socket error occurs </summary>
    public delegate void ErrorHandler(SocketClient socket, Exception exception);

    /// <summary> Called when a socket connection is accepted </summary>
    public delegate void AcceptHandler(SocketClient socket);

    #endregion

    #region Private Properties

    /// <summary> Flag when disposed is called </summary>
    private bool _disposed;
        
    /// <summary> A TcpListener object to accept socket connections </summary>
    private TcpListener _tcpListener;

    /// <summary> Size of the raw buffer for received socket data </summary>
    private int _sizeOfRawBuffer;

    /// <summary> Size of the raw buffer for user purpose </summary>
    private int _sizeOfByteBuffer;

    /// <summary> RetType: A thread to process accepting socket connections </summary>
    private Thread _acceptThread;

    /// <summary> A reference to a user supplied function to be called when a socket message arrives </summary>
    private MessageHandler _messageHandler;

    /// <summary> A reference to a user supplied function to be called when a socket connection is closed </summary>
    private CloseHandler _closeHandler;

    /// <summary> A reference to a user supplied function to be called when a socket error occurs </summary>
    private ErrorHandler _errorHandler;

    /// <summary> A reference to a user supplied function to be called when a socket connection is accepted </summary>
    private AcceptHandler _acceptHandler;

    /// <summary> RefTypeArray: An Array of SocketClient objects </summary>
    private readonly List<SocketClient> _socketClientList = [];

    #endregion

    #region Public Properties

    /// <summary> The IpAddress to either connect to or listen on </summary>
    public string IpAddress;

    /// <summary> The Port to either connect to or listen on </summary>
    public int Port = int.MinValue;

    /// <summary> A reference to a user defined object to be passed through the handler functions </summary>
    public object UserArg;

    #endregion

    #region Constructor

    /// <summary> Constructor </summary>
    public SocketServer()
    {
    }

    /// <summary> Dispose function to shutdown the SocketManager </summary>
    public void Dispose()
    {
        try
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        catch
        {
        }
    }
        
    /// <summary> Dispose the server </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        // Check to see if Dispose has already been called.
        if (!_disposed)
        {
            // Note disposing has been done.
            _disposed = true;

            // If disposing equals true, dispose all managed
            // and unmanaged resources.
            if (disposing)
            {
                // Stop the server if the thread is running
                if (_acceptThread != null)
                {
                    Stop();
                }
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary> Function to process and accept socket connection requests </summary>
    private void AcceptConnections()
    {
        Socket socket = null;

        try
        {
            IPAddress useIpAddress = null;
            IPHostEntry hostEntries = Dns.GetHostEntry(IpAddress);
            foreach (IPAddress address in hostEntries.AddressList)
            {
                // Find the IPv4 address first
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    useIpAddress = address;
                    break;
                }
            }
                    
            // Now just use the first address
            if (useIpAddress == null)
            {
                useIpAddress = hostEntries.AddressList[0];
            }
                
            IpAddress = useIpAddress.ToString();
                    
            // Create a new TCPListner and start it up
            _tcpListener = new(useIpAddress, Port);
            _tcpListener.Start();

            for (;;)
            {
                try
                {
                    // If a client connects, accept the connection.
                    socket = _tcpListener.AcceptSocket();
                }

                catch (System.Net.Sockets.SocketException e)
                {
                    // Did we stop the TCPListener
                    if (e.ErrorCode != 10004)
                    {
                        // Call the error handler
                        _errorHandler(null, e);
                        _errorHandler(null, new("Waiting for new connection 1"));

                        // Close the socket down if it exists
                        if (socket != null)
                        {
                            if (socket.Connected)
                            {
                                socket.Dispose();
                            }
                        }
                    }
                    else
                    {
                        _errorHandler(null, new("Shutting Down Accept Thread"));
                        break;
                    }
                }

                catch (Exception e)
                {
                    // Call the error handler
                    _errorHandler(null, e);
                    _errorHandler(null, new("Waiting for new connection 2"));

                    // Close the socket down if it exists
                    if (socket != null)
                    {
                        if (socket.Connected)
                        {
                            socket.Dispose();
                        }
                    }
                }

                try
                {
                    if (socket.Connected)
                    {
                        string remoteEndPoint = socket.RemoteEndPoint.ToString();

                        // Create a SocketClient object
                        SocketClient clientSocket = new(this,
                                                        socket,
                                                        (remoteEndPoint.Length < 15) ? string.Empty : remoteEndPoint.Substring(0, 15),
                                                        Port,
                                                        _sizeOfRawBuffer,
                                                        _sizeOfByteBuffer,
                                                        UserArg,
                                                        new(_messageHandler),
                                                        new(_closeHandler),
                                                        new(_errorHandler));
                        // Add it to the list
                        lock (_socketClientList)
                        {
                            _socketClientList.Add(clientSocket);
                        }

                        // Call the Accept Handler
                        _acceptHandler(clientSocket);

                        // Wait for a message
                        clientSocket.Receive();
                    }
                }

                catch (Exception e)
                {
                    // Call the error handler
                    _errorHandler(null, e);
                    _errorHandler(null, new("Waiting for new connection 3"));
                }
            }
        }

        catch (Exception e)
        {
            // Call the error handler
            _errorHandler(null, e);
            _errorHandler(null, new("Shutting Down Accept Thread"));

            // Close the socket down if it exists
            if (socket != null)
            {
                if (socket.Connected)
                {
                    socket.Dispose();
                }
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary> Function to start the SocketServer </summary>
    /// <param name="ipAddress"> The IpAddress to listening on </param>
    /// <param name="port"> The Port to listen on </param>
    /// <param name="sizeOfRawBuffer"> Size of the Raw Buffer </param>
    /// <param name="sizeOfByteBuffer"> Size of the byte buffer </param>
    /// <param name="userArg"> User supplied arguments </param>
    /// <param name="messageHandler"> Function pointer to the user MessageHandler function </param>
    /// <param name="acceptHandler"> Function pointer to the user AcceptHandler function </param>
    /// <param name="closeHandler"> Function pointer to the user CloseHandler function </param>
    /// <param name="errorHandler"> Function pointer to the user ErrorHandler function </param>
    public void Start(string ipAddress, int port, int sizeOfRawBuffer, int sizeOfByteBuffer, object userArg,
                      MessageHandler messageHandler, AcceptHandler acceptHandler, CloseHandler closeHandler,
                      ErrorHandler errorHandler)
    {
        // Is an AcceptThread currently running
        if (_acceptThread == null)
        {
            // Set connection values
            IpAddress = ipAddress;
            Port = port;

            // Save the Handler Functions
            _messageHandler = messageHandler;
            _acceptHandler = acceptHandler;
            _closeHandler = closeHandler;
            _errorHandler = errorHandler;

            // Save the buffer size and user arguments
            _sizeOfRawBuffer = sizeOfRawBuffer;
            _sizeOfByteBuffer = sizeOfByteBuffer;
            UserArg = userArg;

            // Start the listening thread if one is currently not running
            ThreadStart tsThread = AcceptConnections;
            _acceptThread = new(tsThread) { Name = $"SocketAccept-{ipAddress}" };
            _acceptThread.Start();
        }
    }

    /// <summary> Function to stop the SocketServer.  It can be restarted with Start </summary>
    public void Stop()
    {
        // Abort the accept thread
        if (_acceptThread != null)
        {
            _tcpListener.Stop();
            _acceptThread.Join();
            _acceptThread = null;
        }

        lock (_socketClientList)
        {
            // Dispose of all of the socket connections
            foreach (SocketClient socketClient in _socketClientList)
            {
                socketClient.Dispose();
            }
        }

        // Wait for all of the socket client objects to be destroyed
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Empty the Socket Client List
        _socketClientList.Clear();

        // Clear the Handler Functions
        _messageHandler = null;
        _acceptHandler = null;
        _closeHandler = null;
        _errorHandler = null;

        // Clear the buffer size and user arguments
        _sizeOfRawBuffer = 0;
        UserArg = null;
    }

    /// <summary> Funciton to remove a socket from the list of sockets </summary>
    /// <param name="socketClient"> A reference to a socket to remove </param>
    public void RemoveSocket(SocketClient socketClient)
    {
        try
        {
            lock (_socketClientList)
            {
                // Remove ths client socket object from the list
                _socketClientList.Remove(socketClient);
            }
        }

        catch (Exception exception)
        {
            _errorHandler(socketClient, exception);
        }
    }

    /// <summary> Called to retrieve the socket object by the Socket Index </summary>
    /// <param name="socketIndex"></param>
    public SocketClient RetrieveSocket(Int32 socketIndex)
    {
        SocketClient socketClient = null;

        try
        {
            lock (_socketClientList)
            {
                // If the server index exists, return it
                socketClient = _socketClientList.FirstOrDefault(k => k.SocketIndex == socketIndex);
            }
        }

        catch (Exception)
        {
        }

        return socketClient;
    }

    /// <summary> Called to send a message to call socket clients </summary>
    /// <param name="rawBuffer"></param>
    public void SendAll(Byte[] rawBuffer)
    {
        lock (_socketClientList)
        {
            // If the server index exists, return it
            foreach (SocketClient socketClient in _socketClientList)
            {
                socketClient.Send(rawBuffer);
            }
        }
    }
        
    /// <summary> Called to send a message to call socket clients </summary>
    /// <param name="message"></param>
    public void SendAll(string message)
    {
        lock (_socketClientList)
        {
            // If the server index exists, return it
            foreach (SocketClient socketClient in _socketClientList)
            {
                socketClient.Send(message);
            }
        }
    }
        
    #endregion
}

#endregion