﻿#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> UserData class contains the user data of PDU </summary>
public class UserData
{
    #region Private Properties
        
    /// <summary> The octets of the text message </summary>
    private List<byte> _shortMessage;

    #endregion
        
    #region Public Properties
        
    /// <summary> The octets of the text message </summary>
    public byte[] ShortMessage
    {
        get => _shortMessage.ToArray();

        set => _shortMessage = [..value];
    }

    /// <summary> GSM User Data Header information in the short message </summary>
    public UserDataHeaderCollection Headers { get; set; }
        
    #endregion
        
    #region Constructor
        
    /// <summary> Constructor </summary>
    private UserData()
    {
        _shortMessage = [];
        Headers = [];
    }
		
    #endregion

    #region Helper Methods

    /// <summary> Called to convert the short message into a string </summary>
    /// <param name="defaultEncoding"></param>
    /// <param name="dataCoding"></param>
    /// <returns> string </returns>
    public string ShortMessageText(DataCodings defaultEncoding, DataCodings dataCoding)
    {
        SmppBuffer smppBuff = new(defaultEncoding, ShortMessage);

        return smppBuff.ExtractEncodedString(dataCoding);
    }

    #endregion

    #region Build Method

    /// <summary>Builds the complete User Data Header (UDH)</summary>
    public byte[] Build()
    {
        List<byte> udhBuffer = new();

        foreach (UserDataHeader header in Headers)
        {
            udhBuffer.AddRange(header.GetBytes());
        }

        return udhBuffer.ToArray();
    }

    #endregion


    #region Factory Methods

    /// <summary> Called to create a UserData object </summary>
    /// <returns> UserData </returns>
    public static UserData Create()
    {
        return new();
    }

    /// <summary> Called to create a UserData object </summary>
    /// <param name="buf"></param>
    /// <param name="udhi"></param>
    /// <returns> UserData</returns>
    public static UserData Create(SmppBuffer buf, bool udhi)
    {
        return buf.ExtractUserData(udhi, 0);
    }

    #endregion

    #region Public Properties

    /// <summary> Called to add a new user data object to the buffer </summary>
    /// <param name="userData"></param>
    public void Add(UserData userData)
    {
        foreach (UserDataHeader userDataHeader in userData.Headers)
        {
            Headers.Add(userDataHeader);
        }

        _shortMessage.AddRange(userData.ShortMessage);
    }

    #endregion
}