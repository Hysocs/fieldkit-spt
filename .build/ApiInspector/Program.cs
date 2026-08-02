using System.Reflection;
using System.Runtime.Loader;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Emit;

var runtime = args.Length > 0 ? args[0] : @"C:\SPT\SPT_Runtime";
if (args.Length > 2 && args[2] == "metadata")
{
    using var stream = File.OpenRead(Path.Combine(runtime, args[1]));
    using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader();
    foreach (var handle in reader.TypeDefinitions)
    {
        var definition = reader.GetTypeDefinition(handle);
        var name = reader.GetString(definition.Name);
        var declaringName = "";
        if (!definition.GetDeclaringType().IsNil)
            declaringName = reader.GetString(reader.GetTypeDefinition(definition.GetDeclaringType()).Name);
        if (name != "ObjectsFactory" &&
            !(declaringName == "ObjectsFactory" && name == "Pools") &&
            !name.Contains("ResourceInfo", StringComparison.OrdinalIgnoreCase) &&
            !name.Contains("PoolResource", StringComparison.OrdinalIgnoreCase)) continue;
        Console.WriteLine($"METATYPE {declaringName}+{name}");
        foreach (var methodHandle in definition.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            var parameterCount = method.GetParameters().Count - 1;
            Console.WriteLine($"  METAMETHOD {reader.GetString(method.Name)} params={parameterCount}");
        }
        foreach (var fieldHandle in definition.GetFields())
            Console.WriteLine($"  METAFIELD {reader.GetString(reader.GetFieldDefinition(fieldHandle).Name)}");
    }
    return;
}
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(runtime, name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};

var pattern = args.Length > 1 ? args[1] : "SPTarkov*.dll";
foreach (var file in Directory.GetFiles(runtime, pattern))
{
    try
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
        if (args.Length > 2 && args[2] == "movement-il")
        {
            var moveType = assembly.GetType("EFT.MovePlayerState", true)!;
            foreach (var method in moveType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(m => m.Name is "Move" or "UpdateMovementDirection" or "ProcessDirection" or "SetSmoothDiscreteDirection" or "SetMovementDiscreteDirection" or "TransformDirection"))
                DumpIl(method);
            return;
        }
        var poolsType = assembly.GetType("EFT.ObjectsFactory+Pools", false);
        if (poolsType is not null)
            foreach (var method in poolsType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(m => m.Name == "ConvertResourceInfo"))
                Console.WriteLine($"POOLMETHOD {method.ReturnType.FullName} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName))})");
        Type[] types;
        try { types = assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray(); }
        foreach (var type in types.Where(t =>
                     t.Name.Contains("ModMetadata", StringComparison.OrdinalIgnoreCase) ||
                     t.Name is "ItemHelper" or "ProfileHelper" or "MailSendService" or "ActiveHealthController" ||
                     t.Name is "MovementContext" or "EffectsController" or "SimpleCharacterController" ||
                     t.Name is "ObjectsFactory" ||
                     (t.DeclaringType?.Name == "ObjectsFactory" && (t.Name == "Pools" || t.Name.Contains("Resource"))) ||
                     t.Name.Contains("RunState", StringComparison.OrdinalIgnoreCase) ||
                     t.Name.Contains("JumpState", StringComparison.OrdinalIgnoreCase) ||
                     Enumerable.Repeat(t, 1).SelectMany(x => { var chain = new List<Type>(); for (var b = x.BaseType; b is not null; b = b.BaseType) chain.Add(b); return chain; }).Any(b => b.FullName == "EFT.BaseMovementState") ||
                     (t.Name.Contains("Fall", StringComparison.OrdinalIgnoreCase) && t.Namespace?.StartsWith("EFT") == true)))
        {
            Console.WriteLine($"TYPE {type.Assembly.GetName().Name}: {type.FullName}");
            if (type.Name.Contains("ModMetadata", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  KIND interface={type.IsInterface} abstract={type.IsAbstract}");
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    Console.WriteLine($"  PROPERTY {property.PropertyType.FullName} {property.Name}");
            }
            if (type.Name is "ActiveHealthController" || type.Name.Contains("Fall", StringComparison.OrdinalIgnoreCase))
            {
                if (type.Name is "ActiveHealthController")
                    foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                 .Where(p => p.Name.Contains("Player", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Fall", StringComparison.OrdinalIgnoreCase)))
                        Console.WriteLine($"  PROPERTY {property.PropertyType.FullName} {property.Name}");
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .Where(m => m.Name.Contains("Fall", StringComparison.OrdinalIgnoreCase)))
                    Console.WriteLine($"  METHOD {method.ReturnType.FullName} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})");
            }
            if (type.Name is "MovementContext" or "EffectsController" or "SimpleCharacterController")
            {
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                             .Where(p => p.Name is "CurrentState" or "State"))
                    Console.WriteLine($"  PROPERTY {property.PropertyType.FullName} {property.Name}");
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                             .Where(m => m.Name.StartsWith("method_") || m.Name.Contains("Motion") || m.Name.Contains("Sprint") || m.Name.Contains("Pose") || m.Name.Contains("Walk") || m.Name is "Move"))
                    Console.WriteLine($"  METHOD {method.ReturnType.FullName} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})");
            }
            if (type.Name is "MovePlayerState" or "JumpPlayerState")
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Console.WriteLine($"  FIELD {field.FieldType.FullName} {field.Name}");
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Console.WriteLine($"  METHOD {method.ReturnType.FullName} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})");
            }
            if (type.Name is "SimpleCharacterController" or "ObjectsFactory" or "Pools" || type.DeclaringType?.Name == "ObjectsFactory")
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Console.WriteLine($"  FIELD {field.FieldType.FullName} {field.Name}");
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    Console.WriteLine($"  METHOD {method.ReturnType.FullName} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name))})");
            }
        }
        var fallMethod = types.FirstOrDefault(t => t.FullName == "EFT.HealthSystem.ActiveHealthController")?
            .GetMethod("HandleFall", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [typeof(float)], null);
        if (fallMethod is not null)
        {
            var token = BitConverter.GetBytes(fallMethod.MetadataToken);
            Console.WriteLine($"FALL TOKEN 0x{fallMethod.MetadataToken:X8}");
            foreach (var owner in types)
            foreach (var method in owner.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                byte[]? il;
                try { il = method.GetMethodBody()?.GetILAsByteArray(); } catch { continue; }
                if (il is null) continue;
                for (var i = 0; i <= il.Length - token.Length; i++)
                    if (il.AsSpan(i, token.Length).SequenceEqual(token))
                    {
                        Console.WriteLine($"CALLER {owner.FullName}.{method.Name}");
                        break;
                    }
            }
        }
    }
    catch (Exception ex) { Console.Error.WriteLine($"SKIP {Path.GetFileName(file)}: {ex.GetType().Name}"); }
}

static void DumpIl(MethodInfo method)
{
    Console.WriteLine($"ILMETHOD {method}");
    var bytes = method.GetMethodBody()?.GetILAsByteArray();
    if (bytes is null) return;
    var one = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!).Where(o => o.Size == 1).ToDictionary(o => (byte)o.Value);
    var two = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(f => (OpCode)f.GetValue(null)!).Where(o => o.Size == 2).ToDictionary(o => (byte)(o.Value & 0xff));
    for (var i = 0; i < bytes.Length;)
    {
        var offset = i;
        OpCode op = bytes[i++] == 0xfe ? two[bytes[i++]] : one[bytes[i - 1]];
        object? operand = null;
        var size = 0;
        switch (op.OperandType)
        {
            case OperandType.ShortInlineI: operand = (sbyte)bytes[i]; size = 1; break;
            case OperandType.InlineI: operand = BitConverter.ToInt32(bytes, i); size = 4; break;
            case OperandType.InlineI8: operand = BitConverter.ToInt64(bytes, i); size = 8; break;
            case OperandType.ShortInlineR: operand = BitConverter.ToSingle(bytes, i); size = 4; break;
            case OperandType.InlineR: operand = BitConverter.ToDouble(bytes, i); size = 8; break;
            case OperandType.ShortInlineBrTarget: operand = offset + op.Size + 1 + (sbyte)bytes[i]; size = 1; break;
            case OperandType.InlineBrTarget: operand = offset + op.Size + 4 + BitConverter.ToInt32(bytes, i); size = 4; break;
            case OperandType.ShortInlineVar: operand = bytes[i]; size = 1; break;
            case OperandType.InlineVar: operand = BitConverter.ToUInt16(bytes, i); size = 2; break;
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineType:
            case OperandType.InlineTok:
            case OperandType.InlineString:
                var token = BitConverter.ToInt32(bytes, i); size = 4;
                try { operand = op.OperandType == OperandType.InlineString ? method.Module.ResolveString(token) : method.Module.ResolveMember(token); }
                catch { operand = $"0x{token:X8}"; }
                break;
            case OperandType.InlineSwitch:
                var count = BitConverter.ToInt32(bytes, i); size = 4 + count * 4; operand = $"switch({count})"; break;
        }
        i += size;
        Console.WriteLine($"  {offset:X4}: {op.Name} {operand}");
    }
}
