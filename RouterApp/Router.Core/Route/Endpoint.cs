using System.Reflection;

namespace Router.Core.Route;

public sealed class Endpoint
{
    public Delegate Action { get; }
    public Type[] ArgTypes { get; }
    public string[] ParamNames { get; }
    public bool IsAsync { get; }
    public bool TakesCancellationToken { get; }

    private readonly int[] _segmentToArgIndex;
    private readonly int _cancellationTokenArgIndex;

    public Endpoint(Delegate action, IReadOnlyList<string> segmentNames, IReadOnlyList<Type> segmentTypes)
    {
        if (segmentNames.Count != segmentTypes.Count)
            throw new ArgumentException("segmentNames и segmentTypes должны быть одной длины");

        Action = action;
        var parameters = action.Method.GetParameters();
        ArgTypes = new Type[parameters.Length];
        ParamNames = new string[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            ArgTypes[i] = parameters[i].ParameterType;
            ParamNames[i] = parameters[i].Name ?? string.Empty;
        }

        IsAsync = IsActionReturnTask();
        TakesCancellationToken = ArgTypes.Length > 0
                                 && ArgTypes[ArgTypes.Length - 1] == typeof(CancellationToken);
        _cancellationTokenArgIndex = TakesCancellationToken ? ArgTypes.Length - 1 : -1;

        _segmentToArgIndex = new int[segmentNames.Count];
        for (var i = 0; i < segmentNames.Count; i++)
        {
            var argIndex = ResolveArgIndex(segmentNames[i]);
            var argType = ArgTypes[argIndex];
            var segType = segmentTypes[i];
            if (!IsCompatible(segType, argType))
            {
                throw new ArgumentException(
                    $"Тип сегмента \"{segmentNames[i]}\" ({segType.Name}) несовместим с типом параметра делегата \"{ParamNames[argIndex]}\" ({argType.Name}).");
            }
            _segmentToArgIndex[i] = argIndex;
        }

        ValidateNoExtraParams();
    }

    private static bool IsCompatible(Type segType, Type argType)
    {
        if (argType == segType) return true;
        if (argType.IsAssignableFrom(segType)) return true;
        if (argType == typeof(object)) return true;
        return false;
    }

    private void ValidateNoExtraParams()
    {
        var used = new HashSet<int>(_segmentToArgIndex);
        for (var i = 0; i < ParamNames.Length; i++)
        {
            if (i == _cancellationTokenArgIndex) continue;
            if (!used.Contains(i))
            {
                throw new ArgumentException(
                    $"У делегата есть параметр \"{ParamNames[i]}\", которому нет соответствующего сегмента в шаблоне.");
            }
        }
    }

    private bool IsActionReturnTask()
    {
        var returnType = Action.Method.ReturnType;
        if (returnType == typeof(void)) return false;
        return returnType == typeof(Task) || returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    private int ResolveArgIndex(string segmentName)
    {
        for (var i = 0; i < ParamNames.Length; i++)
        {
            if (i == _cancellationTokenArgIndex) continue;
            if (string.Equals(ParamNames[i], segmentName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        throw new ArgumentException(
            $"В шаблоне есть сегмент \"{{{segmentName}}}\", но у делегата нет параметра с таким именем.");
    }

    public object?[] BuildArgs(IReadOnlyList<object?> segmentValues)
    {
        var args = new object?[ArgTypes.Length];
        for (var i = 0; i < _segmentToArgIndex.Length; i++)
        {
            args[_segmentToArgIndex[i]] = segmentValues[i];
        }
        if (TakesCancellationToken)
            args[_cancellationTokenArgIndex] = CancellationToken.None;
        return args;
    }

    public object?[] BuildArgs(IReadOnlyList<object?> segmentValues, CancellationToken ct)
    {
        var args = BuildArgs(segmentValues);
        if (TakesCancellationToken)
            args[_cancellationTokenArgIndex] = ct;
        return args;
    }
}
