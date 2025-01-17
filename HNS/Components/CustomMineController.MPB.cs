using UnityEngine;

namespace HNS.Components;

public partial class CustomMineController
{
    private const string _SHADER_PROP_EMISSIVE_COLOR = "_EmissiveColor";
    
    public static readonly MaterialPropertyBlock MPB_SEEKER = new MaterialPropertyBlock();
    
    private static MaterialPropertyBlock _mpbHider;
    public static MaterialPropertyBlock MPB_HIDER
    {
        get
        {
            if (_mpbHider == null)
            {
                _mpbHider = new MaterialPropertyBlock();
                _mpbHider.SetColor(_SHADER_PROP_EMISSIVE_COLOR, new Color(0f, 0.5f, 1f, 0.2f));
            }

            return _mpbHider;
        }
    }

    private static MaterialPropertyBlock _mpbOther;
    public static MaterialPropertyBlock MPB_OTHER
    {
        get
        {
            if (_mpbOther == null)
            {
                _mpbOther = new MaterialPropertyBlock();
                _mpbOther.SetColor(_SHADER_PROP_EMISSIVE_COLOR, new Color(0.2f, 1f, 0.2f, 0.2f));
            }

            return _mpbOther;
        }
    }
    
    private static MaterialPropertyBlock _mpbDisabled;
    public static MaterialPropertyBlock MPB_DISABLED
    {
        get
        {
            if (_mpbDisabled == null)
            {
                _mpbDisabled = new MaterialPropertyBlock();
                _mpbDisabled.SetColor(_SHADER_PROP_EMISSIVE_COLOR, COL_STATE_DISABLED);
            }

            return _mpbDisabled;
        }
    }
    
    private static MaterialPropertyBlock _mpbHacked;
    public static MaterialPropertyBlock MPB_HACKED
    {
        get
        {
            if (_mpbHacked == null)
            {
                _mpbHacked = new MaterialPropertyBlock();
                _mpbHacked.SetColor(_SHADER_PROP_EMISSIVE_COLOR, COL_STATE_HACKED);
            }

            return _mpbHacked;
        }
    }
}