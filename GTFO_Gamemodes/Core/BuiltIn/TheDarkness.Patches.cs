using Gamemodes.Net;
using HarmonyLib;
using Player;
using SNetwork;

namespace Gamemodes.Core.BuiltIn;

public partial class TheDarkness
{
    private static Harmony _harmonyInstance;
    private static bool _patched;
    
    private static void Patch()
    {
        _harmonyInstance ??= new Harmony("Gamemodes.TheDarkness");

        if (_patched)
            return;

        _patched = true;
        
        _harmonyInstance.Patch(AccessTools.Method(typeof(PLOC_Jump), nameof(PLOC_Jump.Enter)),
            postfix: new HarmonyMethod(AccessTools.Method(typeof(TheDarkness), nameof(OnLocalPlayerJump))));

        _harmonyInstance.Patch(AccessTools.Method(typeof(CameraManager), nameof(CameraManager.OnFocusStateChanged)),
            postfix: new HarmonyMethod(
                AccessTools.Method(typeof(TheDarkness), nameof(OnCameraManagerFocusStateChanged))));
    }

    private static void Unpatch()
    {
        if (!_patched)
            return;
        
        _patched = false;
        
        _harmonyInstance.UnpatchSelf();
    }

    public static void OnCameraManagerFocusStateChanged(eFocusState state)
    {
        if (state != eFocusState.FPS)
            return;

        if (!PlayerManager.TryGetLocalPlayerAgent(out var player))
            return;

        var blinder = player.gameObject.GetComponent<Blinder>();

        if (blinder == null)
            return;
        
        blinder.BlindPlayer();
    }
    
    public static void OnLocalPlayerJump()
    {
        if (!_isTimeToBecomeTheChosenOne)
            return;

        _isTimeToBecomeTheChosenOne = false;
        
        NetworkingManager.SendEvent(new pChosenOne()
        {
            ChosenPlayerID = NetworkingManager.LocalPlayerId,
        }, SNet.Master);
    }
}