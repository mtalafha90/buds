namespace BudsControl.Protocols.Samsung;

/// <summary>Request = a command either side initiates; Response = an acknowledgement/reply to one. Confirmed via the reference source's MsgTypes enum (Request = 0, Response = 1).</summary>
public enum SppMsgType : byte
{
    Request = 0,
    Response = 1,
}

public sealed record SppFrame(SppMsgType Type, SamsungMessageId MessageId, byte[] Payload);
