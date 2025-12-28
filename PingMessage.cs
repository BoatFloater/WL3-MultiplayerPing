using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.Networking;

namespace MultiplayerPing;

public class PingMessage : MessageBase
{
    public Vector3 Position;
    public Color32 Color;

    public PingMessage(IntPtr ptr) : base(ptr)
    {
    }

    public PingMessage() : base(ClassInjector.DerivedConstructorPointer<PingMessage>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(Position);
        writer.Write(Color);
    }

    public override void Deserialize(NetworkReader reader)
    {
        Position = reader.ReadVector3();
        Color = reader.ReadColor32();
    }
}
