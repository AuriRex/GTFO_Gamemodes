using System;
using Gamemodes.Net;
using UnityEngine;

namespace Gamemodes.UI;

public record TeamDisplayData(char Identifier, Color Color, Func<PlayerWrapper, string> UpdateExtraInfo = null);