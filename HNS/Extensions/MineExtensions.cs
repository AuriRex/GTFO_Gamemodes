using HNS.Components;

namespace HNS.Extensions;

public static class MineExtensions
{
    public static CustomMineController GetController(this MineDeployerInstance mine)
    {
        return mine?.gameObject.GetComponent<CustomMineController>();
    }
}