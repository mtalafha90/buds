namespace BudsControl.Protocols.Samsung;

public sealed record SppFrame(SamsungMessageId MessageId, byte[] Payload);
