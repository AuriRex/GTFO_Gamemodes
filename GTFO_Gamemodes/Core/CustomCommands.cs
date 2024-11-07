using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gamemodes.Core;

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

        if (!methodInfo.IsStatic)
        {
            throw new ArgumentException($"Method \"{methodInfo.Name}\" ({command}) must be static", nameof(methodInfo));
        }
        
        if (!_commands.TryAdd(command, methodInfo))
        {
            throw new ArgumentException($"Command \"{command}\" already registered!", nameof(command));
        }

        return this;
    }

    public bool Get(string command, out MethodInfo methodInfo)
    {
        return _commands.TryGetValue(command, out methodInfo);
    }
}
