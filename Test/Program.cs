using AradSMPP.Net;

Console.WriteLine("Hello, World!");

const string? server = "127.0.0.1"; // IP Address or Name of the server
const short port = 2775; // Port
const string? shortLongCode = "9890001234"; // The short or long code for this bind
const string? systemId = "test"; // The system id for authentication
const string? password = "test"; // The password of authentication
const DataCodings dataCoding = DataCodings.Ascii; // The encoding to use if Default is returned in any PDU or encoding request

// Create a esme manager to communicate with an ESME
EsmeManager? connectionManager = new("Test",
                                     shortLongCode,
                                     ConnectionEventHandler,
                                     ReceivedMessageHandler,
                                     ReceivedGenericNackHandler,
                                     SubmitMessageHandler,
                                     QueryMessageHandler,
                                     LogEventHandler,
                                     PduDetailsHandler);

// Bind one single Receiver connection
connectionManager.AddConnections(1, ConnectionModes.Receiver, server, port, systemId, password, "Receiver", dataCoding);

// Bind one Transmitter connection
connectionManager.AddConnections(1, ConnectionModes.Transmitter, server, port, systemId, password, "Transceiver", dataCoding);

// Accept command input
bool bQuit = false;

for (; ; )
{
    // Hit Enter in the terminal once the binds are up to see this prompt

    Console.WriteLine("Commands");
    Console.WriteLine("send 989123456789 علی عدالت عزیز، دوره آزمایشی پلن طرح پرمیموم شما به اتمام رسیده است. برای حفظ دسترسی به امکانات کامل، همین حالا پلن خود را تمدید کنید.");
    Console.WriteLine("quit");
    Console.WriteLine("");

    Console.Write("\n#>");

    string? command = Console.ReadLine();
    if (command is { Length: 0 })
    {
        continue;
    }

    switch (command?.Split(' ')[0].ToString())
    {
        case "quit":
        case "exit":
            bQuit = true;
            break;

        default:
            ProcessCommand(command);
            break;
    }

    if (bQuit)
    {
        break;
    }
}

connectionManager.Dispose();

void ProcessCommand(string? command)
{
    string[]? parts = command?.Split(' ');

    switch (parts?[0])
    {
        case "send":
            SendMessage(command);
            break;

        case "query":
            QueryMessage(command);
            break;
    }
}

void SendMessage(string? command)
{
    string?[]? parts = command?.Split(' ');
    string? phoneNumber = parts?[1];

    if (parts != null)
    {
        string message = string.Join(" ", parts, 2, parts.Length - 2);

        // This is set in the Submit PDU to the SMSC
        // If you are responding to a received message, make this the same as the received message
        const DataCodings submitDataCoding = DataCodings.Ucs2;

        // Use this to encode the message
        // We need to know the actual encoding.
        const DataCodings encodeDataCoding = DataCodings.Ucs2;

        // There is a default encoding set for each connection. This is used if the encodeDataCoding is Default

        connectionManager.SendMessageLarge(phoneNumber, null, Ton.National, Npi.Isdn, submitDataCoding, encodeDataCoding, message, out List<SubmitSm> submitSm, out List<SubmitSmResp> submitSmResp);
        int i = 0;
        foreach (SubmitSmResp resp in submitSmResp)  
        {
            Console.Write("submitSm:{0}, submitSmResp:{1}, messageId:{2}", submitSm[i].DestAddr, resp.Status, resp.MessageId);
            i++;
        }

        
    }
}

void QueryMessage(string? command)
{
    string?[] parts = command.Split(' ');
    string? messageId = parts[1];

    QuerySm? querySm = connectionManager.SendQuery(messageId);
    Console.WriteLine(querySm.Status.ToString());
}

static void ReceivedMessageHandler(string logKey, MessageTypes messageType, string serviceType, Ton sourceTon, Npi sourceNpi, string shortLongCode, DateTime dateReceived, string phoneNumber, DataCodings dataCoding, string message)
{
    if (messageType == MessageTypes.SmscDeliveryReceipt)
    {
        Console.WriteLine("This is the message for the status of delivery");
        Console.WriteLine("MessageType: " + messageType.ToString());
        Console.WriteLine("ReceivedMessageHandler: {0}", message);
    }
    else
    {
        Console.Write("This is normal message");
        Console.WriteLine("MessageType: " + messageType.ToString());
        Console.WriteLine("ReceivedMessageHandler: {0}", message);
    }
}

static void ReceivedGenericNackHandler(string logKey, int sequence)
{
}

static void SubmitMessageHandler(string logKey, int sequence, string messageId)
{
    Console.WriteLine("SubmitMessageHandler: {0}", messageId);
}

static void QueryMessageHandler(string logKey, int sequence, string messageId, DateTime finalDate, int messageState, long errorCode)
{
    Console.WriteLine("QueryMessageHandler: {0} {1} {2}", messageId, finalDate, messageState);
}

static void LogEventHandler(LogEventNotificationTypes logEventNotificationType, string logKey, string shortLongCode, string message)
{
    Console.WriteLine(message);
}

static void ConnectionEventHandler(string logKey, ConnectionEventTypes connectionEventType, string message)
{
    Console.WriteLine("ConnectionEventHandler: {0} {1}", connectionEventType, message);
}

static Guid? PduDetailsHandler(string logKey, PduDirectionTypes pduDirectionType, Header pdu, List<PduPropertyDetail> details)
{
    Guid? pduHeaderId = null;

    try
    {
        // Do not store these
        if ((pdu.Command == CommandSet.EnquireLink) || (pdu.Command == CommandSet.EnquireLinkResp))
        {
            return null;
        }

        string connectionString = null; // If null InsertPdu will just log to stdout
        int serviceId = 0;              // Internal Id used to track multiple SMSC systems

        // InsertPdu in DB (logKey, connectionString, serviceId, pduDirectionType, details, pdu.PduData.BreakIntoDataBlocks(4096), out pduHeaderId);
    }

    catch (Exception exception)
    {
        Console.WriteLine("{0}", exception.Message);
    }

    return pduHeaderId;
}
