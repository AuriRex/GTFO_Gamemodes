using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ExtraModes.Net.Packets;
using Gamemodes.Core;
using Gamemodes.Extensions;
using Gamemodes.Net;
using SNetwork;
using UnityEngine;

namespace ExtraModes.Net;

public class NetBoxManager
{
    private ushort _latestBoxID;
    private readonly Dictionary<ushort, BoxRef> _boxes = new();
    private readonly NetEvents _net;
    
    public NetBoxManager(NetEvents net)
    {
        _net = net;
        net.RegisterEvent<pBoxAction>(OnBoxReceived);
    }

    public void CreateBoxCustom(Vector3 position, Quaternion rotation, Vector3 scale, bool invisible = false)
    {
        _latestBoxID++;
        if (_latestBoxID >= ushort.MaxValue)
            _latestBoxID = 0;
        
        SendBoxAll(new pBoxAction()
        {
            Action = (byte) (invisible ? BoxAction.CreateOrRepositionButInvisible : BoxAction.CreateOrReposition),
            ID = _latestBoxID,
            Position = position,
            Rotation = rotation,
            Scale = scale,
        });
    }
    
    public void CreateBox(Vector3 position, BoxType boxType, bool invisible = false)
    {
        if (!SNet.IsMaster)
            return;

        Vector3 scale = Vector3.one;
        Quaternion rotation = Quaternion.identity;
        
        switch (boxType)
        {
            case BoxType.Custom:
                break;
            case BoxType.Floor1X1:
                scale = new Vector3(1f, 0.1f, 1f);
                position -= Vector3.up * 0.1f;
                break;
            case BoxType.Wall1X1:
                scale = new Vector3(1f, 1f, 0.1f);
                break;
            case BoxType.Wall1X1_TWO:
                scale = new Vector3(0.1f, 1f, 1f);
                break;
        }
        
        CreateBoxCustom(position, rotation, scale, invisible);
    }

    public enum BoxType
    {
        Custom,
        Floor1X1,
        Wall1X1,
        Wall1X1_TWO
    }

    private void SendBoxAll(pBoxAction data)
    {
        SendBox(data, null, invokeLocal: true);
    }
    
    private void SendBox(pBoxAction data, SNet_Player targetPlayer = null, bool invokeLocal = false)
    {
        _net.SendEvent(data, targetPlayer, invokeLocal);
    }
    
    private void OnBoxReceived(ulong sender, pBoxAction data)
    {
        NetworkingManager.GetPlayerInfo(sender, out var info);

        if (!info.IsMaster)
            return;
        
        OnBoxAction(data);
    }
    
    public void OnBoxAction(pBoxAction data)
    {
        var inDict = _boxes.TryGetValue(data.ID, out var boxRef);

        switch ((BoxAction) data.Action)
        {
            case BoxAction.CreateOrReposition:
                boxRef ??= BoxRef.CreateNewBox(data);
                boxRef.Translate(data);
                boxRef.SetVisible(true);
                break;
            case BoxAction.CreateOrRepositionButInvisible:
                boxRef ??= BoxRef.CreateNewBox(data);
                boxRef.Translate(data);
                boxRef.SetVisible(false);
                break;
            case BoxAction.Delete:
                boxRef?.Cleanup();
                boxRef = null;
                if(_boxes.ContainsKey(data.ID))
                    _boxes.Remove(data.ID);
                break;
        }
        
        if (!inDict && boxRef != null)
            _boxes[data.ID] = boxRef;
    }

    public void MasterHandleLateJoiner(SNet_Player lateJoiner)
    {
        if (!SNet.IsMaster)
            return;

        CoroutineManager.StartCoroutine(SendLateJoinBoxCommands(lateJoiner).WrapToIl2Cpp());
    }

    private IEnumerator SendLateJoinBoxCommands(SNet_Player targetPlayer)
    {
        var boxes = _boxes.Values.ToArray();

        const int YIELD_COUNT = 15;
        var count = 0;
        foreach (var boxRef in boxes)
        {
            var action = boxRef.GetBoxAction();

            SendBox(action, targetPlayer, invokeLocal: false);
            
            count++;
            if (count > YIELD_COUNT)
                yield return null;
        }
    }

    public void Cleanup()
    {
        foreach (var kvp in _boxes)
        {
            kvp.Value.Cleanup();
        }
        
        _boxes.Clear();
    }

    private class BoxRef
    {
        public ushort ID;
        private readonly GameObject _box;
        private readonly Renderer _renderer;
        private bool _destroyed;
        private bool _isVisible = true;

        private BoxRef(pBoxAction data)
        {
            ID = data.ID;
            _box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _renderer = _box.GetComponent<Renderer>();
            
            Translate(data);
        }

        public void Translate(pBoxAction data)
        {
            if (_destroyed)
                return;
            
            var trans = _box.transform;
            
            trans.position = data.Position;
            trans.rotation = data.Rotation;
            trans.localScale = data.Scale;
        }

        public void SetVisible(bool visible)
        {
            if (_destroyed)
                return;

            _isVisible = visible;
            _renderer.forceRenderingOff = !visible;
        }
        
        public void Cleanup()
        {
            _box.SafeDestroy();
            _destroyed = true;
        }

        public static BoxRef CreateNewBox(pBoxAction data)
        {
            return new BoxRef(data);
        }

        public pBoxAction GetBoxAction()
        {
            var trans = _box.transform;
            return new pBoxAction
            {
                ID = ID,
                Action = (byte)(_isVisible ? BoxAction.CreateOrReposition : BoxAction.CreateOrRepositionButInvisible),
                Position = trans.position,
                Rotation = trans.rotation,
                Scale = trans.localScale,
            };
        }
    }
    
    public enum BoxAction
    {
        CreateOrReposition,
        CreateOrRepositionButInvisible,
        Delete
    }
}