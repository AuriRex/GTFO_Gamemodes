using Gamemodes.Extensions;
using Gamemodes.Net;
using HNS.Components;
using HNS.Extensions;
using HNS.Net;

namespace HNS.Core;

public class MineStateManager
{
    public static void ProcessIncomingAction(PlayerWrapper sender, MineDeployerInstance mine, MineState mineState)
    {
        Plugin.L.LogWarning($"MineStateManager.ProcessIncomingAction: State:{mineState}, Sender:{sender.NickName}, Mine:{mine?.name}");

        var controller = mine.GetController();
        
        switch (mineState)
        {
            default:
            case MineState.DoNotChange:
                break;
            case MineState.Alarm:
                controller.StartAlarmSequence(sender.PlayerAgent);
                break;
            case MineState.Detecting:
                controller.StateDetect();
                break;
            case MineState.Disabled:
                controller.StateDisable();
                break;
            case MineState.Hacked:
                controller.StartHackedSequence(sender.PlayerAgent);
                break;
        }
    }
}