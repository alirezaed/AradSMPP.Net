# AradSMPP.Net

**High-performance SMPP Client Library for .NET 8+**  
An open-source SMPP client implementation written in C#.  

Supports:
- SMPP 3.4 protocol
- Multiple connections (Receiver / Transmitter / Transceiver)
- Async architecture
- Message submission & delivery
- Query messages
- Custom event handlers
- Extensible architecture

---

## 🚀 Getting Started

### Installation

Clone this repository:

```bash
git clone https://github.com/yourusername/AradSMPP.Net.git
```

Or add the project to your solution.

---

## ⚡ Basic Example

```csharp
using AradSMPP.Net;

Console.WriteLine("Hello, World!");

const string? server = "127.0.0.1";
const short port = 2775;
const string? shortLongCode = "9890001234";
const string? systemId = "test";
const string? password = "test";
const DataCodings dataCoding = DataCodings.Ascii;

EsmeManager? connectionManager = new("Test",
                                     shortLongCode,
                                     ConnectionEventHandler,
                                     ReceivedMessageHandler,
                                     ReceivedGenericNackHandler,
                                     SubmitMessageHandler,
                                     QueryMessageHandler,
                                     LogEventHandler,
                                     PduDetailsHandler);

connectionManager.AddConnections(1, ConnectionModes.Receiver, server, port, systemId, password, "Receiver", dataCoding);
connectionManager.AddConnections(1, ConnectionModes.Transmitter, server, port, systemId, password, "Transmitter", dataCoding);

bool bQuit = false;

for (; ; )
{
    Console.WriteLine("Commands");
    Console.WriteLine("send 989123456789 Hello");
    Console.WriteLine("quit\n");

    Console.Write("\n#>");
    string? command = Console.ReadLine();
    if (command is { Length: 0 })
        continue;

    switch (command?.Split(' ')[0])
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
        break;
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

        const DataCodings submitDataCoding = DataCodings.Default;
        const DataCodings encodeDataCoding = DataCodings.Ascii;

        connectionManager.SendMessage(phoneNumber, null, Ton.National, Npi.Isdn, submitDataCoding, encodeDataCoding, message, out SubmitSm? submitSm, out SubmitSmResp? submitSmResp);

        Console.WriteLine($"submitSm: {submitSm?.DestAddr}, submitSmResp: {submitSmResp?.Status}, messageId: {submitSmResp?.MessageId}");
    }
}

void QueryMessage(string? command)
{
    string?[] parts = command.Split(' ');
    string? messageId = parts[1];

    QuerySm? querySm = connectionManager.SendQuery(messageId);
    Console.WriteLine(querySm?.Status.ToString());
}
```

---

## 📡 Event Handlers

```csharp
static void ReceivedMessageHandler(string logKey, MessageTypes messageType, string serviceType, Ton sourceTon, Npi sourceNpi, string shortLongCode, DateTime dateReceived, string phoneNumber, DataCodings dataCoding, string message)
{
    if (messageType == MessageTypes.SmscDeliveryReceipt)
    {
        Console.WriteLine("Delivery Receipt");
    }
    else
    {
        Console.WriteLine($"Received Message: {message}");
    }
}

static void ReceivedGenericNackHandler(string logKey, int sequence) { }

static void SubmitMessageHandler(string logKey, int sequence, string messageId)
{
    Console.WriteLine($"SubmitMessageHandler: {messageId}");
}

static void QueryMessageHandler(string logKey, int sequence, string messageId, DateTime finalDate, int messageState, long errorCode)
{
    Console.WriteLine($"QueryMessageHandler: {messageId} {finalDate} {messageState}");
}

static void LogEventHandler(LogEventNotificationTypes logEventNotificationType, string logKey, string shortLongCode, string message)
{
    Console.WriteLine(message);
}

static void ConnectionEventHandler(string logKey, ConnectionEventTypes connectionEventType, string message)
{
    Console.WriteLine($"ConnectionEventHandler: {connectionEventType} {message}");
}

static Guid? PduDetailsHandler(string logKey, PduDirectionTypes pduDirectionType, Header pdu, List<PduPropertyDetail> details)
{
    Guid? pduHeaderId = null;

    try
    {
        if ((pdu.Command == CommandSet.EnquireLink) || (pdu.Command == CommandSet.EnquireLinkResp))
            return null;
    }
    catch (Exception exception)
    {
        Console.WriteLine($"{exception.Message}");
    }

    return pduHeaderId;
}
```

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🤝 Contributions

Contributions are welcome!  
Feel free to fork this project, open issues, or submit pull requests.

---

✅ **Enjoy using AradSMPP.Net!** 🚀
