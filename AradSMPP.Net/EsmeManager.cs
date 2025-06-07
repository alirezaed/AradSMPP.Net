namespace AradSMPP.Net;

/// <summary> Provides ESME (Extended Short Message Entity) Management </summary>
public class EsmeManager : IDisposable
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
    public delegate void ReceivedMessageHandler(string logKey, MessageTypes messageType, string serviceType, Ton sourceTon, Npi sourceNpi, string shortLongCode, DateTime dateReceived, string phoneNumber, DataCodings dataCoding, string message);

    /// <summary> Called when a submit message is acknowledged </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    public delegate void ReceivedGenericnackHandler(string logKey, int sequence);

    /// <summary> Called when a submit message is acknowledged </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    public delegate void SubmitMessageHandler(string logKey, int sequence, string messageId);

    /// <summary> Called when a query message is responded </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    /// <param name="finalDate"></param>
    /// <param name="messageState"></param>
    /// <param name="errorCode"></param>
    public delegate void QueryMessageHandler(string logKey, int sequence, string messageId, DateTime finalDate, int messageState, long errorCode);

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
        
    /// <summary> The logKey for writing logs to the correct files </summary>
    private readonly string _logKey = string.Empty;

    /// <summary> The short/long code being managed </summary>
    private readonly string? _shortLongCode;


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


    /// <summary> A list of receiver connections </summary>
    private readonly List<EsmeConnection> _receivers = [];

    /// <summary> A dictionary of transmitter connections </summary>
    private readonly Dictionary<int, EsmeConnection> _transmitters = new();

    /// <summary> A pointer to the next transmitter to use </summary>
    private int _nextTransmitter = 1;

    #endregion

    #region Constructor

    /// <summary> Contructor </summary>
    /// <param name="logKey"></param>
    /// <param name="shortLongCode"></param>
    /// <param name="connectionEventHandler"></param>
    /// <param name="receivedMessageHandler"></param>
    /// <param name="receivedGenericNackHandler"></param>
    /// <param name="submitMessageHandler"></param>
    /// <param name="queryMessageHandler"></param>
    /// <param name="logEventHandler"></param>
    /// <param name="pduDetailsEventHandler"></param>
    public EsmeManager(string logKey, string? shortLongCode,
                       ConnectionEventHandler connectionEventHandler, ReceivedMessageHandler receivedMessageHandler,
                       ReceivedGenericnackHandler receivedGenericNackHandler, SubmitMessageHandler submitMessageHandler,
                       QueryMessageHandler queryMessageHandler, LogEventHandler logEventHandler,
                       PduDetailsEventHandler pduDetailsEventHandler)
    {
        _logKey = logKey;
        _shortLongCode = shortLongCode;

        _connectionEventHandler = connectionEventHandler;
        _receivedMessageHandler = receivedMessageHandler;
        _receivedGenericNackHandler = receivedGenericNackHandler;
        _submitMessageHandler = submitMessageHandler;
        _queryMessageHandler = queryMessageHandler;
        _logEventHandler = logEventHandler;
        _pduDetailsEventHandler = pduDetailsEventHandler;
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
        WriteLog("EsmeManager : Dispose : Started");

        if (!Disposed)
        {
            // Note disposing has begun
            Disposed = true;

            try
            {
                WriteLog("EsmeManager : Dispose : Info : Disconnect the receiver connections");

                foreach (EsmeConnection smppConnection in _receivers)
                {
                    smppConnection.Dispose();
                }
            }

            catch (Exception exception)
            {
                WriteLog(LogEventNotificationTypes.Email, "EsmeManager : Dispose : ERROR : {0}", exception.ToString());
            }

            try
            {
                WriteLog("EsmeManager : Dispose : Info : Disconnect the transmitter connections");

                foreach (EsmeConnection smppConnection in _transmitters.Values)
                {
                    smppConnection.Dispose();
                }
            }

            catch (Exception exception)
            {
                WriteLog(LogEventNotificationTypes.Email, "EsmeManager : Dispose : ERROR : {0}", exception.ToString());
            }
        }

        WriteLog("EsmeManager : Dispose : Completed");
    }

    #endregion

    #region Log Methods
        
    /// <summary> Called to write out to the log </summary>
    /// <param name="message"></param>
    private void WriteLog(string message)
    {
        _logEventHandler(LogEventNotificationTypes.None, _logKey, _shortLongCode, message);
    }

    /// <summary> Called to write out to the log </summary>
    /// <param name="message"></param>
    /// <param name="logValues"></param>
    private void WriteLog(string message, params object?[] logValues)
    {
        _logEventHandler(LogEventNotificationTypes.None, _logKey, _shortLongCode, string.Format(message, logValues));
    }

    /// <summary> Called to write out to the log </summary>
    /// <param name="logEventNotificationType"></param>
    /// <param name="message"></param>
    private void WriteLog(LogEventNotificationTypes logEventNotificationType, string message)
    {
        _logEventHandler(logEventNotificationType, _logKey, _shortLongCode, message);
    }

    /// <summary> Called to write out to the log </summary>
    /// <param name="logEventNotificationType"></param>
    /// <param name="message"></param>
    /// <param name="logValues"></param>
    private void WriteLog(LogEventNotificationTypes logEventNotificationType, string message, params object[] logValues)
    {
        _logEventHandler(logEventNotificationType, _logKey, _shortLongCode, string.Format(message, logValues));
    }

    #endregion

    #region Event Methods

    /// <summary> Called when a connection event is fired </summary>
    /// <param name="logKey"></param>
    /// <param name="connectionEventType"></param>
    /// <param name="message"></param>
    public void ConnectionEventConnectionHandler(string logKey, ConnectionEventTypes connectionEventType, string message)
    {
        if (_connectionEventHandler != null)
        {
            _connectionEventHandler(logKey, connectionEventType, message);
        }
    }

    /// <summary> Called when a message is received on a connection </summary>
    /// <param name="logKey"></param>
    /// <param name="serviceType"></param>
    /// <param name="sourceTon"></param>
    /// <param name="sourceNpi"></param>
    /// <param name="shortLongCode"></param>
    /// <param name="dateReceived"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="dataCoding"></param>
    /// <param name="message"></param>
    public void ReceivedMessageConnectionHandler(string logKey, MessageTypes messageType, string serviceType, Ton sourceTon, Npi sourceNpi, string shortLongCode, DateTime dateReceived, string phoneNumber, DataCodings dataCoding, string message)
    {
        if (_receivedMessageHandler != null)
        {
            _receivedMessageHandler(logKey, messageType, serviceType, sourceTon, sourceNpi, shortLongCode, dateReceived, phoneNumber, dataCoding, message);
        }
    }

    /// <summary> A user supplied method to call when a generic nack is received </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    public void ReceivedGenericNackConnectionHandler(string logKey, int sequence)
    {
        if (_receivedGenericNackHandler != null)
        {
            _receivedGenericNackHandler(logKey, sequence);
        }
    }

    /// <summary> Called when a submit message is acknowledged </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    public void SubmitMessageConnectionHandler(string logKey, int sequence, string messageId)
    {
        if (_submitMessageHandler != null)
        {
            _submitMessageHandler(logKey, sequence, messageId);
        }
    }

    /// <summary> Called when a query message is responded </summary>
    /// <param name="logKey"></param>
    /// <param name="sequence"></param>
    /// <param name="messageId"></param>
    /// <param name="finalDate"></param>
    /// <param name="messageState"></param>
    /// <param name="errorCode"></param>
    public void QueryMessageConnectionHandler(string logKey, int sequence, string messageId, DateTime finalDate, int messageState, long errorCode)
    {
        if (_queryMessageHandler != null)
        {
            _queryMessageHandler(logKey, sequence, messageId, finalDate, messageState, errorCode);
        }
    }

    /// <summary> Called to log an event </summary>
    /// <param name="logEventNotificationType"></param>
    /// <param name="shortLongCode"></param>
    /// <param name="message"></param>
    /// <param name="logKey"></param>
    public void LogEventConnectionHandler(LogEventNotificationTypes logEventNotificationType, string logKey, string shortLongCode, string message)
    {
        if (_logEventHandler != null)
        {
            _logEventHandler(logEventNotificationType, logKey, shortLongCode, message);
        }
    }

    /// <summary> Called when a pdu details are available </summary>
    /// <param name="logKey"></param>
    /// <param name="pduDirectionType"></param>
    /// <param name="pdu"></param>
    /// <param name="details"></param>
    /// <returns> External Id </returns>
    private Guid? PduDetailsConnectionHandler(string logKey, PduDirectionTypes pduDirectionType, Header pdu, List<PduPropertyDetail> details)
    {
        Guid? externalId = null;

        if (_pduDetailsEventHandler != null)
        {
            externalId = _pduDetailsEventHandler(logKey, pduDirectionType, pdu, details);
        }

        return externalId;
    }

    #endregion

    #region Connection Management Methods

    /// <summary> Called to add a transceiver connection </summary>
    /// <param name="connectionId"></param>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="logKey"></param>
    /// <param name="defaultEncoding"></param>
    private void AddTransceiverConnection(int connectionId, string? host, int port, string? userName, string? password, string logKey, DataCodings defaultEncoding)
    {
        lock (_receivers)
        {
            // Create the smppConnection object
            EsmeConnection smppConnection = new(connectionId, _shortLongCode, ConnectionModes.Transceiver,
                                                host, port, userName, password, logKey, defaultEncoding,
                                                ConnectionEventConnectionHandler,
                                                ReceivedMessageConnectionHandler,
                                                ReceivedGenericNackConnectionHandler,
                                                SubmitMessageConnectionHandler,
                                                QueryMessageConnectionHandler,
                                                LogEventConnectionHandler,
                                                PduDetailsConnectionHandler);
            
            // Add the connection to both list
            _transmitters.Add(connectionId, smppConnection);
        }
    }

    /// <summary> Called to add a receiver connection </summary>
    /// <param name="connectionId"></param>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="logKey"></param>
    /// <param name="defaultEncoding"></param>
    private void AddReceiverConnection(int connectionId, string? host, int port, string? userName, string? password, string logKey, DataCodings defaultEncoding)
    {
        lock (_receivers)
        {
            // Create the smppConnection object
            EsmeConnection smppConnection = new(connectionId, _shortLongCode, ConnectionModes.Receiver,
                                                host, port, userName, password, logKey, defaultEncoding,
                                                ConnectionEventConnectionHandler,
                                                ReceivedMessageConnectionHandler,
                                                ReceivedGenericNackConnectionHandler,
                                                null,
                                                QueryMessageConnectionHandler,
                                                LogEventConnectionHandler,
                                                PduDetailsConnectionHandler);
            
            // Add the connection to the list
            _receivers.Add(smppConnection);
        }
    }

    /// <summary> Called to add a transmitter connection </summary>
    /// <param name="connectionId"></param>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="logKey"></param>
    /// <param name="defaultEncoding"></param>
    private void AddTransmitterConnection(int connectionId, string? host, int port, string? userName, string? password, string logKey, DataCodings defaultEncoding)
    {
        lock (_transmitters)
        {
            // Create the smppConnection object
            EsmeConnection smppConnection = new(connectionId, _shortLongCode, ConnectionModes.Transmitter,
                                                host, port, userName, password, logKey, defaultEncoding,
                                                ConnectionEventConnectionHandler,
                                                null,
                                                ReceivedGenericNackConnectionHandler,
                                                SubmitMessageConnectionHandler,
                                                QueryMessageConnectionHandler,
                                                LogEventConnectionHandler,
                                                PduDetailsConnectionHandler);
            
            // Add the connection to the list
            _transmitters.Add(connectionId, smppConnection);
        }
    }

    /// <summary> Called to return the next transmitter for sending </summary>
    /// <returns> SmppConnection </returns>
    private EsmeConnection NextTransmitterConnection()
    {
        EsmeConnection smppConnection = null;

        int totalConnections = _transmitters.Count();

        // We only want a bound connection. We will try them all
        for (int connection = 0; connection < totalConnections; ++connection)
        {
            lock (_transmitters)
            {
                smppConnection = _transmitters[_nextTransmitter];

                if (++_nextTransmitter > _transmitters.Count())
                {
                    _nextTransmitter = 1;
                }

                if (smppConnection.Status == ConnectionStatus.Bound)
                {
                    break;
                }

                smppConnection = null;
            }
        }

        return smppConnection;
    }

    #endregion

    #region Public Methods

    /// <summary> Called to add connections </summary>
    /// <param name="howMany"></param>
    /// <param name="connectionMode"></param>
    /// <param name="host"></param>
    /// <param name="port"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <param name="logKey"></param>
    /// <param name="defaultEncoding"></param>
    public void AddConnections(int howMany, ConnectionModes connectionMode, string? host, int port,
                               string? userName, string? password, string logKey, DataCodings defaultEncoding)
    {
        WriteLog("EsmeManager : AddConnections : Started : HowMany[{0}] ConnectionMode[{1}] Host[{2}] Port[{3}] LogKey[{4}] DefaultEncoding[{5}]", howMany, connectionMode, host, port, logKey, defaultEncoding);

        for (int connection = 1; connection <= howMany; ++connection)
        {
            if (connectionMode == ConnectionModes.Transceiver)
            {
                AddTransceiverConnection(connection, host, port, userName, password, logKey, defaultEncoding);
            }
            else if (connectionMode == ConnectionModes.Receiver)
            {
                AddReceiverConnection(connection, host, port, userName, password, logKey, defaultEncoding);
            }
            else
            {
                AddTransmitterConnection(connection, host, port, userName, password, logKey, defaultEncoding);
            }
        }
    }

    /// <summary> Called to send the message </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="serviceType"></param>
    /// <param name="sourceTon"></param>
    /// <param name="sourceNpi"></param>
    /// <param name="submitDataCoding"></param>
    /// <param name="encodeDataCoding"></param>
    /// <param name="message"></param>
    /// <param name="submitSm"></param>
    /// <param name="submitSmResp"></param>
    /// <returns> 1 - Successful / 0 - Failed </returns>
    public int SendMessage(string? phoneNumber, string? serviceType, Ton sourceTon, Npi sourceNpi, DataCodings submitDataCoding, DataCodings encodeDataCoding, string message,  out SubmitSm? submitSm, out SubmitSmResp? submitSmResp)
    {
        int retVal = 0;

        submitSm = null;
        submitSmResp = null;

        try
        {
            // Capture the next transmitter connection
            EsmeConnection smppConnection = NextTransmitterConnection();

            if (smppConnection == null)
            {
                WriteLog("EsmeManager : SendMessage : Warning : Not Bound To The SMPP Server");

                return 2;
            }

            // Send the message
            retVal = smppConnection.SendMessage(phoneNumber, serviceType, sourceTon, sourceNpi, submitDataCoding, encodeDataCoding, message, out submitSm, out submitSmResp);
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeManager : SendMessage : ERROR : {0}", exception.ToString());
        }

        return retVal;
    }

    /// <summary> Called to send the message </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="serviceType"></param>
    /// <param name="sourceTon"></param>
    /// <param name="sourceNpi"></param>
    /// <param name="submitDataCoding"></param>
    /// <param name="encodeDataCoding"></param>
    /// <param name="message"></param>
    /// <param name="submitSmList"></param>
    /// <param name="submitSmRespList"></param>
    /// <returns> 1 - Successful / 0 - Failed </returns>
    public int SendMessageLarge(string? phoneNumber, string? serviceType, Ton sourceTon, Npi sourceNpi, DataCodings submitDataCoding, DataCodings encodeDataCoding, string message, out List<SubmitSm?> submitSmList, out List<SubmitSmResp?> submitSmRespList)
    {
        int retVal = 0;

        submitSmList = null;
        submitSmRespList = null;

        try
        {
            // Capture the next transmitter connection
            EsmeConnection smppConnection = NextTransmitterConnection();

            if (smppConnection == null)
            {
                WriteLog("EsmeManager : SendMessage : Warning : Not Bound To The SMPP Server");

                return 2;
            }

            // Send the message
            retVal = smppConnection.SendMessageLarge(phoneNumber, serviceType, sourceTon, sourceNpi, submitDataCoding, encodeDataCoding, message, out submitSmList, out submitSmRespList);
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeManager : SendMessage : ERROR : {0}", exception.ToString());
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
            // Capture the next transmitter connection
            EsmeConnection smppConnection = NextTransmitterConnection();

            if (smppConnection.Status != ConnectionStatus.Bound)
            {
                WriteLog("EsmeManager : SendMessage : Warning : Not Connected To The SMPP Server");

                return querySm;
            }

            // Send the message
            querySm = smppConnection.SendQuery(messageId);
        }

        catch (Exception exception)
        {
            WriteLog(LogEventNotificationTypes.Email, "EsmeManager : SendMessage : ERROR : {0}", exception.ToString());
        }

        return querySm;
    }

    #endregion
}