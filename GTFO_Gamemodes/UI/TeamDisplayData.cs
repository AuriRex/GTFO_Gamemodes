using System;
using Gamemodes.Net;
using UnityEngine;

namespace Gamemodes.UI;

public record TeamDisplayData(string Prefix, Color Color, Func<PlayerWrapper, string> UpdateExtraInfo = null, bool Hide = false);