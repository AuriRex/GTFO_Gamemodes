using System.Runtime.InteropServices;
using UnityEngine;

namespace Gamemodes.Net.Packets.Data;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pQuaternion
{
    [MarshalAs(UnmanagedType.R4)]
    public float X;
    [MarshalAs(UnmanagedType.R4)]
    public float Y;
    [MarshalAs(UnmanagedType.R4)]
    public float Z;
    [MarshalAs(UnmanagedType.R4)]
    public float W;
    
    public static implicit operator pQuaternion(Quaternion v) => new pQuaternion
    {
        X = v.x,
        Y = v.y,
        Z = v.z,
        W = v.w,
    };
    
    public static implicit operator Quaternion(pQuaternion v) => new Quaternion(v.X, v.Y, v.Z, v.W);
}