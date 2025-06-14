﻿#region Namespaces
#endregion

namespace AradSMPP.Net;

/// <summary> Represents TLV (Tag, Length, Value) format for optional parameters </summary>
public class Tlv
{
    #region Public Properties
        
    /// <summary> The Tag field is used to uniquely identify the particular optional parameter </summary>
    public ushort Tag { get; set; }

    /// <summary> Indicates the length of the Value property in octets </summary>
    public ushort Length { get; set; }

    /// <summary> Contains the actual data for the optional parameter </summary>
    public byte[] Value { get; set; }
		
    /// <summary> Identifies the particular optional parameter </summary>
    public OptionalTags TagValue => (OptionalTags) Tag;
    #endregion
		
    #region Constructor

    /// <summary> Constructor </summary>
    private Tlv()
    {
    }

    /// <summary> Constructor </summary>
    /// <param name="tag"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    private Tlv(ushort tag, ushort length, byte[] value)
    {
        Tag = tag;
        Length = length;
        Value = value;
    }

    /// <summary> Constructor </summary>
    /// <param name="tag"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    private Tlv(OptionalTags tag, ushort length, byte[] value)
    {
        Tag = Convert.ToUInt16(tag);
        Length = length;
        Value = value;
    }

    #endregion

    #region Factory Methods

    /// <summary> Called to create a TLV object </summary>
    /// <returns> TLV </returns>
    public static Tlv Create()
    {
        return new();
    }

    /// <summary> Called to create a TLV object </summary>
    /// <param name="tag"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    /// <returns> TLV </returns>
    public static Tlv Create(ushort tag, ushort length, byte[] value)
    {
        return new(tag, length, value);
    }

    /// <summary> Called to create a TLV object </summary>
    /// <param name="tag"></param>
    /// <param name="length"></param>
    /// <param name="value"></param>
    /// <returns> TLV </returns>
    public static Tlv Create(OptionalTags tag, ushort length, byte[] value)
    {
        return new(tag, length, value);
    }

    #endregion
}