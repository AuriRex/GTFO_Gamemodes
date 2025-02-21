using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gamemodes.Core;

public class CustomCommands
{
    public bool HasExceptions => _exceptions.Count > 0;
    public IReadOnlyList<Exception> CaughtExceptions => _exceptions;

    private readonly List<Exception> _exceptions = new();
    private readonly Dictionary<string, MethodInfo> _commands = new();

    public CustomCommands Add(string command, Func<string[], string> func)
    {
        return Add(command, func.Method);
    }

    public CustomCommands Add(string command, MethodInfo methodInfo)
    {
        command = command.ToLower();

        try
        {
            if (!methodInfo.IsStatic)
            {
                throw new ArgumentException($"Method \"{methodInfo.Name}\" ({command}) must be static", nameof(methodInfo));
            }
        
            if (!_commands.TryAdd(command, methodInfo))
            {
                throw new ArgumentException($"Command \"{command}\" already registered!", nameof(command));
            }
        }
        catch (Exception ex)
        {
            _exceptions.Add(ex);
        }

        return this;
    }

    public bool Get(string command, out MethodInfo methodInfo)
    {
        return _commands.TryGetValue(command, out methodInfo);
    }

    public void LogAnyErrors(Action<string> logErrorAction, Action<string> logStackTraceAction = null)
    {
        if (!HasExceptions)
            return;
        
        foreach (var ex in _exceptions)
        {
            logErrorAction?.Invoke($"{ex.GetType().Name}: {ex.Message}");
            logStackTraceAction?.Invoke($"StackTrace:\n{ex.StackTrace}");
        }
    }
}
