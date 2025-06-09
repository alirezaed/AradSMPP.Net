#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Represents GSM User Data Header information in the short message user data </summary>
public class UserDataHeader
{
    #region Private Properties

    /// <summary> Identifies Information Element in the User Data Header </summary>
    private InformationElementIdentifiers _iei;
        
    /// <summary> Information Element Data </summary>
    private byte[] _data;

    #endregion
        
    #region Public Properties

    /// <summary> Identifies Information Element id </summary>
    private byte IeiId { get; set; }

    /// <summary> Identifies Information Element in the User Data Header </summary>
    public InformationElementIdentifiers Iei { get => _iei;
        set { _iei = value; IeiId = (byte) value; } }
        
    /// <summary> Length of Information Element </summary>
    public byte Length { get; set; }
        
    /// <summary> Information Element Data </summary>
    public byte[] Data { get => _data;
        set { _data = value; Length = Convert.ToByte(_data.Length); } }

    #endregion
        
    #region Constructor
        
    /// <summary> Constructor </summary>
    private UserDataHeader()
    {
    }

    /// <summary> Constructor </summary>
    /// <param name="iei"></param>
    /// <param name="len"></param>
    /// <param name="data"></param>
    private UserDataHeader(byte iei, byte len, byte[] data)
    {
        IeiId = iei;
        Length = len;
        _data = data;
            
        object obj = InformationElementIdentifiers.Parse(typeof(InformationElementIdentifiers), IeiId.ToString());
        if (obj != null)
        {
            _iei = (InformationElementIdentifiers) obj;
        }
        else
        {
            _iei = InformationElementIdentifiers.Unknown;
        }
    }

    /// <summary> Constructor </summary>
    /// <param name="iei"></param>
    /// <param name="len"></param>
    /// <param name="data"></param>
    private UserDataHeader(InformationElementIdentifiers iei, byte len, byte[] data)
    {
        IeiId = Convert.ToByte(iei);
        Length = len;
        _data = data;
        _iei = iei;
    }
        
    #endregion

    #region Factory Methods

    /// <summary> Called to create a UserDataHeader object </summary>
    /// <returns> UserDataHeader </returns>
    internal static UserDataHeader Create()
    {
        return new();
    }

    /// <summary> Called to create a UserDataHeader object </summary>
    /// <param name="iei"></param>
    /// <param name="len"></param>
    /// <param name="data"></param>
    /// <returns> UserDataHeader </returns>
    internal static UserDataHeader Create(byte iei, byte len, byte[] data)
    {
        return new(iei, len, data);
    }

    /// <summary> Called to create a UserDataHeader object </summary>
    /// <param name="iei"></param>
    /// <param name="len"></param>
    /// <param name="data"></param>
    /// <returns> UserDataHeader </returns>
    internal static UserDataHeader Create(InformationElementIdentifiers iei, byte len, byte[] data)
    {
        return new(iei, len, data);
    }

    #endregion

    #region GetBytes Method

    /// <summary>Gets UserDataHeader as bytes (IEI | Length | Data)</summary>
    public byte[] GetBytes()
    {
        byte[] buffer = new byte[2 + Length];

        buffer[0] = IeiId;
        buffer[1] = Length;
        Array.Copy(Data, 0, buffer, 2, Length);

        return buffer;
    }

    #endregion

}