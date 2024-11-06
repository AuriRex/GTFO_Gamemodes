using System.Runtime.InteropServices;
using AIGraph;

namespace Gamemodes.Net.Packets.Data;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pCourseNode
{
    [MarshalAs(UnmanagedType.U2)]
    public ushort CourseNodeIDPlusOne;

    public pCourseNode Set(AIG_CourseNode courseNode)
    {
        if (courseNode != null)
        {
            this.CourseNodeIDPlusOne = (ushort)(courseNode.NodeID + 1);
            return this;
        }
        this.CourseNodeIDPlusOne = 0;
        return this;
    }

    public bool TryGet(out AIG_CourseNode courseNode)
    {
        return AIG_CourseNode.GetCourseNode((this.CourseNodeIDPlusOne - 1), out courseNode);
    }

    public static implicit operator AIG_CourseNode(pCourseNode v)
    {
        v.TryGet(out var value);
        return value;
    }
    
    public static implicit operator pCourseNode(AIG_CourseNode v)
    {
        return new pCourseNode().Set(v);
    }
}