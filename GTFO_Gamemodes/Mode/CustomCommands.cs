using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gamemodes.Mode;

public class CustomCommands
{
    private readonly Dictionary<string, MethodInfo> _commands = new();

    public CustomCommands Add(string command, Func<string[], string> func)
    {
        Add(command, func.Method);

        return this;
    }

    public CustomCommands Add(string command, MethodInfo methodInfo)
    {
        command = command.ToLower();

        if (_commands.ContainsKey(command))
        {
            throw new ArgumentException($"Command \"{command}\" already registered!", nameof(command));
        }

        _commands.Add(command, methodInfo);

        return this;
    }

    public bool Get(string command, out MethodInfo methodInfo)
    {
        return _commands.TryGetValue(command, out methodInfo);
    }
}
