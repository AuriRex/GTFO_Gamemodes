using System.Runtime.InteropServices;
using UnityEngine;

namespace Gamemodes.Net.Packets.Data;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pVector3
{
    [MarshalAs(UnmanagedType.R4)]
    public float PosX;
    [MarshalAs(UnmanagedType.R4)]
    public float PosY;
    [MarshalAs(UnmanagedType.R4)]
    public float PosZ;
    
    public static implicit operator pVector3(Vector3 v) => new pVector3
    {
        PosX = v.x,
        PosY = v.y,
        PosZ = v.z,
    };
    
    public static implicit operator Vector3(pVector3 v) => new Vector3(v.PosX, v.PosY, v.PosZ);
}