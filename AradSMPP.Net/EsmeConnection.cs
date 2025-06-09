#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Manages a single ESME (Extended Short Message Entity) connection </summary>
internal class EsmeConnection : IDisposable
{
    #region Delegates

    /// <summary> Called when a message is received </summary>
    /// <param name="logKey"></param>
    /// <param name="serviceType"></param>
    /// <param name="sourceTon"></param>
    /// <param name="sourceNpi"></param>
    /// <param name="shortLongCode"></param>
    /// <param name="dateReceived"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="dataCoding"></param>
    /// <param name="message"></param>
    public delegate void ReceivedMessageHandler(string logKey, MessageTypes messageType, string? serviceType, Ton sourceTon, Npi sourceNpi, string? shortLongCode, DateTime dateReceived, string? phoneNumber, DataCodings dataCoding, string message);

    /// <summary> Called when a submit message is acknowledged </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    public delegate void ReceivedGenericnackHandler(string logKey, int sequence);

    /// <summary> Called when a submit message is acknowledged </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    public delegate void SubmitMessageHandler(string logKey, int sequence, string? messageId);

    /// <summary> Called when a query message is responded </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    /// <param name="finalDate"></param>
    /// <param name="messageState"></param>
    /// <param name="errorCode"></param>
    public delegate void QueryMessageHandler(string logKey, int sequence, string? messageId, DateTime finalDate, int messageState, long errorCode);

    /// <summary> Called to log an event </summary>
    /// <param name="logEventNotificationType"></param>
    /// <param name="shortLongCode"></param>
    /// <param name="message"></param>
    /// <param name="logKey"></param>
    public delegate void LogEventHandler(LogEventNotificationTypes logEventNotificationType, string logKey, string? shortLongCode, string message);

    /// <summary> Called when a connection event occurrs </summary>
    /// <param name="logKey"></param>
    /// <param name="connectionEventType"></param>
    /// <param name="message"></param>
    public delegate void ConnectionEventHandler(string logKey, ConnectionEventTypes connectionEventType, string message);

    /// <summary> Called to capture the details of the pdu </summary>
    /// <param name="logKey"></param>
    /// <param name="pduDirectionType"></param>
    /// <param name="pdu"></param>
    /// <param name="details"></param>
    /// <returns> External Id </returns>
    public delegate Guid? PduDetailsEventHandler(string logKey, PduDirectionTypes pduDirectionType, Header pdu, List<PduPropertyDetail> details);

    #endregion

    #region Private Properties

    /// <summary> Flag that determines whether this instance has been disposed or not yet </summary>
    protected bool Disposed;
        
    /// <summary> A unique id for logging </summary>
    private readonly int _connectionId;

    /// <summary> The short/long code being managed </summary>
    private readonly string? _shortLongCode;

    /// <summary> The connection mode, Transceiver, Transmitter, Receiver</summary>
    private readonly ConnectionModes _connectionMode;
        
    /// <summary> The host or ipaddress to connect to </summary>
    private readonly string? _host;

    /// <summary> The port to connect to on the server </summary>
    private readonly int _port;

    /// <summary> The username to authenticate </summary>
    private readonly string? _userName = string.Empty;

    /// <summary> The password to authenticate </summary>
    private readonly string? _password = string.Empty;

    /// <summary> The logKey for writing logs to the correct files </summary>
    private readonly string _logKey = string.Empty;


    /// <summary> A user supplied method to call when a message is received </summary>
    private readonly ReceivedMessageHandler _receivedMessageHandler;

    /// <summary> A user supplied method to call when a generic nack is received </summary>
    private readonly ReceivedGenericnackHandler _receivedGenericNackHandler;

    /// <summary> A user supplied method to call when a submit is ascknowledged </summary>
    private readonly SubmitMessageHandler _submitMessageHandler;

    /// <summary> A user supplied method to call when a query is responded </summary>
    private readonly QueryMessageHandler _queryMessageHandler;

    /// <summary> A user supplied method to call to write logs </summary>
    private readonly LogEventHandler _logEventHandler;

    /// <summary> A user supplied method to call for connection events </summary>
    private readonly ConnectionEventHandler _connectionEventHandler;

    /// <summary> A user supplied method to call for pdu detail data </summary>
    private readonly PduDetailsEventHandler _pduDetailsEventHandler;


    /// <summary> The SMPP Client object </summary>
    private SmppClient _client;

    /// <summary> Handle to the thread trying to connect to the server </summary>
    private readonly Thread _connectionThread;

    /// <summary> An event to tell the connection thread to connect </summary>
    private readonly ManualResetEvent _connectEvent = new(true);

    /// <summary> Dictionary of packet data from cell phones </summary>
    private readonly Dictionary<string, SmppClient.UserDataControl> _collector = new();

    /// <summary> Keeps track of the number of bind errors </summary>
    private bool _isBound;
        
    #endregion

    #region Public Properties

    /// <summary> The unique connection id </summary>
    public int ConnectionIdentifier => _connectionId;

    /// <summary> The current status of the connection </summary>
    public ConnectionStatus Status => _client.Status;
    #endregion

    #region Constructor

    /// <summary> Constructor For ESMS mode </summary>
    /// <param name="connectionId"></param>
    /// <param name="shortLongCode"></param>
    /// <param name="connectionMode"></param>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="logKey"></param>
    /// <param name="defaultEncoding"></param>
    /// <param name="connectionEventHandler"></param>
    /// <param name="receivedMessageHandler"></param>
    /// <param name="receivedGenericNackHandler"></param>
    /// <param name="submitMessageHandler"></param>
    /// <param name="queryMessageHandler"></param>
    /// <param name="logEventHandler"></param>
    /// <param name="pduDetailsEventHandler"></param>
    public EsmeConnection(int connectionId, string? shortLongCode, ConnectionModes connectionMode,
                          string? host, int port, string? userName, string? password, string logKey,
                          DataCodings defaultEncoding,
                          ConnectionEventHandler connectionEventHandler,
                          ReceivedMessageHandler receivedMessageHandler,
                          ReceivedGenericnackHandler receivedGenericNackHandler,
                          SubmitMessageHandler submitMessageHandler,
                          QueryMessageHandler queryMessageHandler,
                          LogEventHandler logEventHandler,
                          PduDetailsEventHandler pduDetailsEventHandler)
    {
        // Properties
        _connectionId = connectionId;
        _shortLongCode = shortLongCode;
        _connectionMode = connectionMode;
        _host = host;
        _port = port;
        _userName = userName;
        _password = password;
        _logKey = $"{logKey}-{_connectionMode}-{_connectionId}";

        // Bind user events
        _connectionEventHandler = connectionEventHandler;
        _receivedMessageHandler = receivedMessageHandler;
        _receivedGenericNackHandler = receivedGenericNackHandler;
        _submitMessageHandler = submitMessageHandler;
        _queryMessageHandler = queryMessageHandler;
        _logEventHandler = logEventHandler;
        _pduDetailsEventHandler = pduDetailsEventHandler;

        // Create the connection to the server
        _client = new(defaultEncoding);

        // Bind Internal ESME required events
        _client.ConnectEvent += ClientEventConnect;
        _client.DeliverSmEvent += ClientEventDeliverSm;
        _client.DisconnectEvent += ClientEventDisconnect;
        _client.EnquireLinkSmEvent += ClientEventEnquireLinkSm;
        _client.EnquireLinkSmRespEvent += ClientEventEnquireLinkSmResp;
        _client.ErrorEvent += ClientEventError;
        _client.GenericNackSmEvent += ClientEventGenericNackSm;
        _client.QuerySmRespEvent += ClientEventQuerySmResp;
        _client.SubmitSmRespEvent += ClientEventSubmitSmResp;
        _client.UnBindSmEvent += ClientEventUnBindSm;
        _client.PduDetailsEvent += ClientEventPduDetails;

        // Start a thread to get this connection
        _connectionThread = new(PerformConnectClient);
        _connectionThread.Start();
    }

    /// <summary> Dispose </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary> Dispose </summary>
    /// <param name="disposing"></param>
    protected void Dispose(bool disposing)
    {
        WriteLog("EsmeConnection : Dispose : Started");

        if (!Disposed)
        {
            // Note disposing has begun
            Disposed = true;

            try
            {
                WriteLog("EsmeConnection : Dispose : Info : Wait For Connection Thread To Die");

                // Kill the PerformConnectClient thread
                _connectEvent.Set();
                _connectionThread.Join(5000);

                WriteLog("EsmeConnection : Dispose : Info : Disconnect from smpp Started");

                _client.Dispose();
                _client = null;

                WriteLog("EsmeConnection : Dispose : Info : Disconnect from smpp Completed");
            }

            catch (Exception exception)
            {
                WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : Dispose : ERROR : {0}", exception.ToString());
            }

            // Kill the PerformConnectClient thread
            _connectEvent.Set();
        }

        WriteLog("EsmeConnection : Dispose : Completed");
    }

    #endregion

    #region Log Methods

    /// <summary> Called to write out to the log </summary>
    /// <param name="message"></param>
    private void WriteLog(string message)
    {
        if (_logEventHandler != null)
        {
            _logEventHandler(LogEventNotificationTypes.None, _logKey, _shortLongCode, message);
        }
    }

    /// <summary> Called to write out to the log </summary>
    /// <param name="message"></param>
    /// <param name="logValues"></param>
    private void WriteLog(string message, params object?[] logValues)
    {
        if (_logEventHandler != null)
        {
            _logEventHandler(LogEventNotificationTypes.None, _logKey, _shortLongCode, string.Format(message, logValues));
        }
    }

    /// <summary> Called to write out to the log </summary>
    /// <param name="logEventNotificationType"></param>
    /// <param name="message"></param>
    private void WriteLog(LogEventNotificationTypes logEventNotificationType, string message)
    {
        if (_logEventHandler != null)
        {
            _logEventHandler(logEventNotificationType, _logKey, _shortLongCode, message);
        }
    }

    /// <summary> Called to write out to the log </summary>
    /// <param name="logEventNotificationType"></param>
    /// <param name="message"></param>
    /// <param name="logValues"></param>
    private void WriteLog(LogEventNotificationTypes logEventNotificationType, string message, params object[] logValues)
    {
        if (_logEventHandler != null)
        {
            _logEventHandler(logEventNotificationType, _logKey, _shortLongCode, string.Format(message, logValues));
        }
    }

    #endregion

    #region Private Methods

    /// <summary> Called to connect to the server </summary>
    private void PerformConnectClient()
    {
        for (;;)
        {
            try
            {
                // Wait to be told to connect
                _connectEvent.WaitOne();

                // Are we shutting down
                if (Disposed)
                {
                    WriteLog("EsmeConnection : PerformConnectClient : Info : Killing Thread");
                    break;
                }

                WriteLog("EsmeConnection : PerformConnectClient : Info : Attempting To Connect");

                // Connect to the server
                if (Connect())
                {
                    if (Bind())
                    {
                        // We are good to go
                        _connectEvent.Reset();
                    }
                    else
                    {
                        WriteLog("EsmeConnection : PerformConnectClient : Info : Dropping the Connection");

                        // Drop the connection
                        _client.Disconnect();

                        WriteLog("EsmeConnection : PerformConnectClient : Info : Sleep For 5 Seconds and try again");

                        // Wait five second an try again
                        Thread.Sleep(5000);

                        WriteLog("EsmeConnection : PerformConnectClient : Info : Try again");

                        _connectEvent.Set();
                    }
                }
                else
                {
                    // Wait five second an try again
                    Thread.Sleep(5000);

                    _connectEvent.Set();
                }
            }

            catch (Exception exception)
            {
                WriteLog("EsmeConnection : PerformConnectClient : Info : Dropping the Connection : {0}", exception.Message);

                if (_client == null)
                {
                    WriteLog("EsmeConnection : PerformConnectClient : Info : Killing Thread");
                    break;
                }

                // Drop the connection
                _client.Disconnect();

                WriteLog("EsmeConnection : PerformConnectClient : Info : Sleep For 5 Seconds and try again");

                // Wait five second an try again
                Thread.Sleep(5000);

                WriteLog("EsmeConnection : PerformConnectClient : Info : Try again");

                _connectEvent.Set();
            }
        }
    }

    /// <summary> Called to connect to the SMPP server </summary>
    private bool Connect()
    {
        WriteLog("EsmeConnection : Connect : Started : Host[{0}] Port[{1}]", _host, _port);

        bool retVal = false;

        try
        {
            if (_client.Status == ConnectionStatus.Closed)
            {
                _client.AddrNpi = (byte)Npi.Unknown;
                _client.AddrTon = (byte)Ton.NetworkSpecific;
                _client.SystemType = string.Empty;

                retVal = _client.Connect(_host, _port);

                if (_client.Status != ConnectionStatus.Closed)
                {
                    _connectionEventHandler(_logKey, ConnectionEventTypes.Connected, $"EsmeConnection : Connect : Info : Host[{_host}] Port[{_port}] Connection Established");
                    retVal = true;
                }
                else
                {
                    _connectionEventHandler(_logKey, ConnectionEventTypes.ConnectionAttemptFailed, $"EsmeConnection : Connect : WARNING : Host[{_host}] Port[{_port}] Connection Failure");
                }
            }
            else
            {
                WriteLog("EsmeConnection : Connect : Info : Host[{0}] Port[{1}] Connection Already Established", _host, _port);
                retVal = true;
            }
        }

        catch (Exception exception)
        {
            WriteLog("EsmeConnection : Connect : ERROR : Host[{0}] Port[{1}] Connection Failed {2}", _host, _port, exception.Message);
        }

        WriteLog("EsmeConnection : Connect : Completed : Host[{0}] Port[{1}] RetVal[{2}]", _host, _port, retVal);

        return retVal;
    }

    /// <summary> Called to disconnect to the SMPP server </summary>
    private void Disconnect()
    {
        WriteLog("EsmeConnection : Disconnect : Started : Host[{0}] Port[{1}]", _host, _port);

        try
        {
            if (_client.Status == ConnectionStatus.Bound)
            {
                if (!UnBind())
                {
                    _client.Disconnect();
                }
            }

            if (_client.Status == ConnectionStatus.Open)
            {
                _client.Disconnect();
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : Disconnect : ERROR : {0}", exception.Message);
        }

        WriteLog("EsmeConnection : Disconnect : Completed : Host[{0}] Port[{1}]", _host, _port);
    }

    /// <summary> Called to authenticate the system </summary>
    private bool Bind()
    {
        WriteLog("EsmeConnection : Bind : Started : Host[{0}] Port[{1}]", _host, _port);

        bool retVal = false;

        try
        {
            // Authenticate to the SMPP Server
            BindSmResp? btrp = _client.Bind(_userName, _password, _connectionMode);

            // How did we do
            switch (btrp.Status)
            {
                case CommandStatus.EsmeRok:
                    _isBound = true;
                    _connectionEventHandler(_logKey, ConnectionEventTypes.Bound, $"EsmeConnection : Bind : Info : Host[{_host}] Port[{_port}] Bind Established");

                    retVal = true;
                    break;

                default:
                    _connectionEventHandler(_logKey, ConnectionEventTypes.BindingAttemptFailed, $"EsmeConnection : Bind : ERROR : Host[{_host}] Port[{_port}] Status[{btrp.Status.ToString()}]");
                    break;
            }
        }

        catch (Exception exception)
        {
            WriteLog("EsmeConnection : Bind : ERROR : Host[{0}] Port[{1}] Bind Failed {2}", _host, _port, exception.Message);
        }

        WriteLog("EsmeConnection : Bind : Completed : Host[{0}] Port[{1}] RetVal[{2}]", _host, _port, retVal);

        return retVal;
    }

    /// <summary> Called to logoff the system </summary>
    private bool UnBind()
    {
        WriteLog("EsmeConnection : UnBind : Started : Host[{0}] Port[{1}]", _host, _port);

        bool retValue = false;

        try
        {
            UnBindSmResp? ubtrp = _client.UnBind();

            switch (ubtrp.Status)
            {
                case CommandStatus.EsmeRok:
                    WriteLog("EsmeConnection : UnBind : Info : Host[{0}] Port[{1}] Connection UnBound", _host, _port);
                    retValue = true;
                    break;

                default:
                    WriteLog("EsmeConnection : UnBind : WARNING : Host[{0}] Port[{1}] Status[{2}] Connection UnBound Failure", _host, _port, ubtrp.Status);
                    break;
            }
        }

        catch (Exception exception)
        {
            WriteLog("EsmeConnection : UnBind : ERROR : Host[{0}] Port[{1}] {2}", _host, _port, exception.Message);
        }

        WriteLog("EsmeConnection : Bind : Completed : Host[{0}] Port[{1}] RetValue[{2}]", _host, _port, retValue);

        return retValue;
    }

    /// <summary> Called to add the message segment to the collector dictionary </summary>
    /// <param name="data"></param>
    private void AddMessageSegmentToCollector(DeliverSm data)
    {
        try
        {
            SmppClient.UserDataControl userDataControl = null;
            string key = data.SourceAddr + data.MessageReferenceNumber;

            lock (_collector)
            {
                if (_collector.ContainsKey(key))
                {
                    userDataControl = _collector[key];
                }
                else
                {
                    userDataControl = new(data.TotalSegments);
                    _collector.Add(key, userDataControl);
                }
            }

            // Add the segment to the current
            userDataControl.UserData.Add(data.UserData);
            --userDataControl.SegmentsLeftToReceived;
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : AddMessageSegmentToCollector : ERROR : {0}", exception.ToString());
        }
    }

    /// <summary> Called to verify we received the last segment of data </summary>
    /// <param name="data"></param>
    /// <returns> True or False </returns>
    private bool IsLastSegment(DeliverSm data)
    {
        bool finished = false;

        try
        {
            SmppClient.UserDataControl userDataControl = null;
            string key = data.SourceAddr + data.MessageReferenceNumber;

            lock (_collector)
            {
                if (_collector.ContainsKey(key))
                {
                    userDataControl = _collector[key];
                }
            }

            if (userDataControl != null)
            {
                if (userDataControl.SegmentsLeftToReceived <= 0)
                {
                    finished = true;
                }
            }
            else
            {
                WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : IsLastSegment : ERROR : No User Data Found");
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email,  "EsmeConnection : IsLastSegment : ERROR : {0}", exception.ToString());
        }

        return finished;
    }

    /// <summary> Called to retrieve the full message </summary>
    /// <param name="data"></param>
    /// <returns> The message </returns>
    private string RetrieveFullMessage(DeliverSm data)
    {
        string message = null;

        try
        {
            SmppClient.UserDataControl userDataControl = null;
            string key = data.SourceAddr + data.MessageReferenceNumber;

            lock (_collector)
            {
                if (_collector.ContainsKey(key))
                {
                    userDataControl = _collector[key];

                    _collector.Remove(key);
                }
            }

            if (userDataControl != null)
            {
                message = userDataControl.UserData.ShortMessageText(data.DefaultEncoding, data.DataCoding);
            }
            else
            {
                WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : RetrieveFullMessage : ERROR : No User Data Found");
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : RetrieveFullMessage : ERROR : {0}", exception.ToString());
        }

        return message;
    }

    #endregion

    #region Public Methods

    /// <summary> Called to send the message </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="serviceType"></param>
    /// <param name="destinationTon"></param>
    /// <param name="destinationNpi"></param>
    /// <param name="submitDataCoding"></param>
    /// <param name="encodeDataCoding"></param>
    /// <param name="message"></param>
    /// <param name="submitSm"></param>
    /// <param name="submitSmResp"></param>
    /// <returns> 0 - Successful / 1 - Failed / 2 - Not Connected </returns>
    public int SendMessage(string? phoneNumber, string? serviceType, Ton destinationTon, Npi destinationNpi, DataCodings submitDataCoding, DataCodings encodeDataCoding, string message, out SubmitSm? submitSm, out SubmitSmResp? submitSmResp)
    {
        int retVal = 1;

        submitSm = null;
        submitSmResp = null;
            
        try
        {
            if (_client.Status != ConnectionStatus.Bound)
            {
                WriteLog("EsmeConnection : SendMessage : Warning : Not Connected To The SMPP Server");

                return 2;
            }

            // The message to send
            string sendMessage = null;

            // Do we need to cut the message down
            if (encodeDataCoding == DataCodings.Ucs2)
            {
                // UCS2 only supports 140 bytes
                if (message.Length > 70)
                {
                    WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : SendMessage : WARNING : Truncating UCS2 message to 70 characters.");

                    // The default is Unicode so truncate the message
                    sendMessage = message.Substring(0, 70);
                }
            }
            else
            {
                if (message.Length > 160)
                {
                    WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : SendMessage : WARNING : Truncating Default message to 160 characters.");

                    sendMessage = message.Substring(0, 160);
                }
            }

            // Prepare the message, I have made sure there is only ever one message
            // with the trunacting above
            submitSm = _client.PrepareSubmit(
                SubmitMode.ShortMessage,
                serviceType,
                (byte) Ton.NetworkSpecific,
                (byte) Npi.Unknown,
                _shortLongCode,
                (byte) destinationTon,
                (byte) destinationNpi,
                phoneNumber,
                submitDataCoding,
                encodeDataCoding,
                (sendMessage == null) ? message : sendMessage);

            // Send the message
            submitSmResp = _client.Submit(submitSm);

            // Log the send call
            WriteLog("EsmeConnection : SendMessage : Send : Sequence[{0}] Phone[{1}] Status[{2}]", submitSmResp.Sequence, phoneNumber, submitSmResp.Status);

            // Was it successful
            if (submitSmResp.Status != CommandStatus.EsmeRok)
            {
                WriteLog("EsmeConnection : SendMessage : ERROR : Failed For Unknown Reason");

                retVal = 1;
            }

            // Success
            retVal = 0;
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : SendMessage : ERROR : {0}", exception.ToString());

            retVal = 1;
        }

        return retVal;
    }

    /// <summary> Called to send the message </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="serviceType"></param>
    /// <param name="destinationTon"></param>
    /// <param name="destinationNpi"></param>
    /// <param name="submitDataCoding"></param>
    /// <param name="encodeDataCoding"></param>
    /// <param name="message"></param>
    /// <param name="submitSmList"></param>
    /// <param name="submitSmRespList"></param>
    /// <returns> 0 - Successful / 1 - Failed / 2 - Not Connected </returns>
    public int SendMessageLarge(string? phoneNumber, string? serviceType, Ton destinationTon, Npi destinationNpi, DataCodings submitDataCoding, DataCodings encodeDataCoding, string message, out List<SubmitSm?> submitSmList, out List<SubmitSmResp?> submitSmRespList)
    {
        int retVal = 1;

        submitSmList = null;
        submitSmRespList = null;

        try
        {
            if (_client.Status != ConnectionStatus.Bound)
            {
                WriteLog("EsmeConnection : SendMessageLarge : Warning : Not Connected To The SMPP Server");

                return 2;
            }

            // Prepare the message, I have made sure there is only ever one message
            // with the trunacting above
            submitSmList = _client.PrepareSubmitLarge(
                SubmitMode.Payload,
                serviceType,
                (byte) Ton.NetworkSpecific,
                (byte) Npi.Unknown,
                _shortLongCode,
                (byte) destinationTon,
                (byte) destinationNpi,
                phoneNumber,
                submitDataCoding,
                encodeDataCoding,
                message);

            // Send the message
            submitSmRespList = _client.Submit(submitSmList);

            foreach (SubmitSmResp? submitSmResp in submitSmRespList)
            {
                // Log the send call
                WriteLog("EsmeConnection : SendMessage : Send : Sequence[{0}] Phone[{1}] Status[{2}]", submitSmResp.Sequence, phoneNumber, submitSmResp.Status);

                // Was it successful
                if (submitSmResp.Status != CommandStatus.EsmeRok)
                {
                    WriteLog("EsmeConnection : SendMessage : ERROR : Failed For Unknown Reason");
                }
            }

            // Success
            retVal = 0;
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : SendMessage : ERROR : {0}", exception.ToString());

            retVal = 1;
        }

        return retVal;
    }
        
    /// <summary> Called to send a query </summary>
    /// <param name="messageId"></param>
    /// <returns> 1 - Successful / 0 - Failed </returns>
    public QuerySm? SendQuery(string? messageId)
    {
        QuerySm? querySm = null;

        try
        {
            if (_client.Status != ConnectionStatus.Bound)
            {
                WriteLog("EsmeConnection : SendQuery : Warning : Not Connected To The SMPP Server");

                return querySm;
            }
                
            // Prepare the query
            querySm = QuerySm.Create(_client.DefaultEncoding, messageId, (byte) Ton.NetworkSpecific, (byte) Npi.Unknown, _shortLongCode);
                
            // Send the query
            QuerySmResp? querySmResp = _client.Query(querySm);

            // Log the send call
            WriteLog("EsmeConnection : SendQueryThroughSMPP : Send : MessageId[{0}] Sequence[{1}] Status[{2}]", messageId, querySm.Sequence, querySmResp.Status);

            // Was it successful
            if (querySmResp.Status != CommandStatus.EsmeRok)
            {
                WriteLog("EsmeConnection : SendQueryThroughSMPP : ERROR : Failed For Unknown Reason");
                    
                querySm = null;
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : SendQueryThroughSMPP : ERROR : {0}", exception.ToString());
        }

        return querySm;
    }

    #endregion

    #region SMPP Event Methods

    /// <summary> Called when a connection is established </summary>
    /// <param name="sender"></param>
    /// <param name="bSuccess"></param>
    private void ClientEventConnect(object sender, bool bSuccess)
    {
    }

    /// <summary> Called when a disconnect occurs </summary>
    /// <param name="sender"></param>
    private void ClientEventDisconnect(object sender)
    {
        try
        {
            if (_isBound)
            {
                _isBound = false;

                if (_connectionEventHandler != null)
                {
                    _connectionEventHandler(_logKey, ConnectionEventTypes.Disconnected, $"EsmeConnection : ClientEventDisconnect : WARNING : Host[{_host}] Port[{_port}] Connection Disconnected");
                }
            }

            // Do we need to try and connect again
            if (Disposed == false)
            {
                _connectEvent.Set();
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : ClientEventDisconnect : ERROR : {0}", exception.ToString());

            // Do we need to try and connect again
            if (Disposed == false)
            {
                _connectEvent.Set();
            }
        }
    }

    /// <summary> Called when a message is received from the SMPP server </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private CommandStatus ClientEventDeliverSm(object sender, DeliverSm data)
    {
        WriteLog("EsmeConnection : ClientEventDeliverSm : Started : SegmentNumber[{0}]", data.SeqmentNumber);

        CommandStatus commandStatus = CommandStatus.EsmeRok;

        try
        {
            string message = null;

            if (data.SeqmentNumber > 0)
            {
                // There are more than 1 seqments to the outbound message

                AddMessageSegmentToCollector(data);

                string? logMessage = string.Format("ServiceType[{0}] DestAddr[{1}] SourceAddr[{2}] MessageReferenceNumber[{3}] Sequence[{4}] SeqmentNumber[{5}] TotalSegments[{6}] DataCoding[{7}] MessageText[{8}]",
                                                   data.ServiceType,
                                                   data.DestAddr,
                                                   data.SourceAddr,
                                                   data.MessageReferenceNumber,
                                                   data.Sequence,
                                                   data.SeqmentNumber,
                                                   data.TotalSegments,
                                                   data.DataCoding,
                                                   data.UserData.ShortMessageText(data.DefaultEncoding, data.DataCoding));

                WriteLog("EsmeConnection : ClientEventDeliverSm : Info : Partial Message : {0}", logMessage);

                if (IsLastSegment(data))
                {
                    message = RetrieveFullMessage(data);
                }
            }
            else
            {
                // There is only 1 seqment to the outbound message
                message = data.UserData.ShortMessageText(data.DefaultEncoding, data.DataCoding);
            }

            if ((message != null) && (_receivedMessageHandler != null))
            {
                // Message has been received
                _receivedMessageHandler(_logKey, data.MessageType, data.ServiceType, (Ton) data.SourceTon, (Npi) data.SourceNpi, _shortLongCode, DateTime.Now, data.SourceAddr, data.DataCoding, message);
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : ClientEventDeliverSm : ERROR : {0}", exception.ToString());
        }

        return commandStatus;
    }

    /// <summary> Called when the SMPP Server receives a sent message </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private void ClientEventSubmitSmResp(object sender, SubmitSmResp data)
    {
        try
        {
            WriteLog("EsmeConnection : ClientEventSubmitSmResp : Info : Sequence[{0}] Status[{1}]", data.Sequence, data.Status);

            if (_submitMessageHandler != null)
            {
                _submitMessageHandler(_logKey, (int) data.Sequence, data.MessageId);
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : ClientEventSubmitSmResp : ERROR : {0}", exception.ToString());
        }
    }
        
    /// <summary> Called when the SMPP query is returned </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private void ClientEventQuerySmResp(object sender, QuerySmResp data)
    {
        try
        {
            WriteLog("EsmeConnection : ClientEventQuerySmResp : Info : MessageId[{0}] MessageState[{1}] Status[{2}]", data.MessageId, data.MessageState, data.Status);

            if (_queryMessageHandler != null)
            {
                _queryMessageHandler(_logKey, (int) data.Sequence, data.MessageId, data.FinalDate, (int) data.MessageState, (long) data.ErrorCode);
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : ClientEventQuerySmResp : ERROR : {0}", exception.ToString());
        }
    }

    /// <summary> Called when an enquire link is received </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private CommandStatus ClientEventEnquireLinkSm(object sender, EnquireLinkSm data)
    {
        WriteLog("EsmeConnection : ClientEventEnquireLinkSm : Info : Enquire Link : Command[{0}] Length[{1}] Sequence[{2}] Status[{3}]", data.Command, data.Length, data.Sequence, data.Status);

        return CommandStatus.EsmeRok;
    }

    /// <summary> Called when an enquire link resp is received </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private void ClientEventEnquireLinkSmResp(object sender, EnquireLinkSmResp data)
    {
        WriteLog("EsmeConnection : ClientEventEnquireLinkSmResp : Info : Enquire Link Resp : Command[{0}] Length[{1}] Sequence[{2}] Status[{3}]", data.Command, data.Length, data.Sequence, data.Status);
    }

    /// <summary> Called when a generic nack is received </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private void ClientEventGenericNackSm(object sender, GenericNackSm data)
    {
        WriteLog("EsmeConnection : ClientEventGenericNackSm : Info : Received Generic Nack  Status[{0}]", data.Status);

        try
        {
            // Generic Nack has been received
            if (_receivedGenericNackHandler != null)
            {
                _receivedGenericNackHandler(_logKey, (int) data.Sequence);
            }
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeConnection : ClientEventGenericNackSm : ERROR : {0}", exception.ToString());
        }
    }

    /// <summary> Called when an error occurs </summary>
    /// <param name="sender"></param>
    /// <param name="comment"></param>
    /// <param name="ex"></param>
    private void ClientEventError(object sender, string? comment, Exception? ex)
    {
        if (ex != null)
        {
            WriteLog("EsmeConnection : ClientEventError : Info : {0} : {1}", comment, ex.Message);
        }
        else
        {
            WriteLog("EsmeConnection : ClientEventError : ERROR : {0}", comment);
        }
    }

    /// <summary> Called when an unbind command is complete </summary>
    /// <param name="sender"></param>
    /// <param name="data"></param>
    private CommandStatus ClientEventUnBindSm(object sender, UnBindSm data)
    {
        if (_connectionEventHandler != null)
        {
            _connectionEventHandler(_logKey, ConnectionEventTypes.UnBound, string.Format("EsmeConnection : ClientEventUnBindSm : WARNING : Host[{0}] Port[{1}] Connection UnBound", _host, _port, _connectionMode, _connectionId));
        }

        return CommandStatus.EsmeRok;
    }

    /// <summary> Called when a pdu details are available </summary>
    /// <param name="send"></param>
    /// <param name="pduDirectionType"></param>
    /// <param name="pdu"></param>
    /// <param name="details"></param>
    /// <returns> External Id </returns>
    private Guid? ClientEventPduDetails(object send, PduDirectionTypes pduDirectionType, Header pdu, List<PduPropertyDetail> details)
    {
        Guid? externalId = null;

        if (_pduDetailsEventHandler != null)
        {
            externalId = _pduDetailsEventHandler(_logKey, pduDirectionType, pdu, details);
        }

        return externalId;
    }

    #endregion
}