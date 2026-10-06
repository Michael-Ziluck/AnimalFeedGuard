using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;

internal static class Program
{
    private static int checks;

    private static int Main(string[] args)
    {
        string game = args[0];
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string directory in new[] { "valheim_Data/Managed", "BepInEx/core" })
            {
                string path = Path.Combine(game, directory, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try { Run(); return 0; }
        catch (Exception error) { System.Console.Error.WriteLine(error); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        Type plugin = typeof(AnimalFeedGuard.Plugin);
        Type patch = plugin.GetNestedType("AutomaticPickupPatch", BindingFlags.NonPublic)!;
        MethodInfo transpiler = AccessTools.Method(patch, "Transpiler");
        MethodInfo filter = AccessTools.Method(plugin, "CanAutoPickup");
        FieldInfo flag = AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_autoPickup));
        MethodInfo target = AccessTools.Method(typeof(Player), "AutoPickup");
        Assert(target != null && target.ReturnType == typeof(void) && target.GetParameters().Single().ParameterType == typeof(float), "pickup patch target signature");
        Assert(filter.ReturnType == typeof(bool) && filter.GetParameters().Single().ParameterType == typeof(ItemDrop), "replacement has the same stack signature as the flag read");

        List<CodeInstruction> original = PatchProcessor.GetOriginalInstructions(target);
        int guard = original.FindIndex(i => i.LoadsField(flag));
        List<CodeInstruction> rewritten = Rewrite(transpiler, original);
        Assert(rewritten.Count == original.Count, "instruction count preserved");
        Assert(rewritten.Count(i => i.Calls(filter)) == 1, "filter applied exactly once to real game IL");
        Assert(rewritten.All(i => !i.LoadsField(flag)), "pickup flag read replaced");
        Assert(rewritten[guard + 1].opcode.FlowControl == System.Reflection.Emit.FlowControl.Cond_Branch, "rejection still skips the item");
        Assert(rewritten.Any(i => i.operand is MethodInfo m && m.Name == "RequestOwn"), "ordinary ownership request remains");
        Assert(rewritten.Any(i => i.operand is MethodInfo m && m.Name == "Pickup"), "ordinary collection remains");

        var generator = new System.Reflection.Emit.DynamicMethod("pickupLabels", typeof(void), Type.EmptyTypes).GetILGenerator();
        var label = generator.DefineLabel();
        var guardInstruction = new CodeInstruction(System.Reflection.Emit.OpCodes.Ldfld, flag);
        guardInstruction.labels.Add(label);
        guardInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        var labelled = Rewrite(transpiler, new[] { guardInstruction }).Single();
        Assert(labelled.labels.Single() == label, "replacement retains branch labels");
        Assert(labelled.blocks.Single().blockType == ExceptionBlockType.BeginExceptionBlock, "replacement retains exception boundaries");

        Reject(transpiler, Array.Empty<CodeInstruction>(), "missing pickup guard rejected");
        Reject(transpiler, new[] { new CodeInstruction(System.Reflection.Emit.OpCodes.Ldfld, flag), new CodeInstruction(System.Reflection.Emit.OpCodes.Ldfld, flag) }, "ambiguous pickup guards rejected");
        System.Console.WriteLine($"PASS: {checks} actual game-IL patch checks. Unity and live pickup behavior are not exercised.");
    }

    private static List<CodeInstruction> Rewrite(MethodInfo method, IEnumerable<CodeInstruction> instructions) =>
        ((IEnumerable<CodeInstruction>)method.Invoke(null, new object[] { instructions })!).ToList();

    private static void Reject(MethodInfo method, IEnumerable<CodeInstruction> instructions, string message)
    {
        try { Rewrite(method, instructions); }
        catch (InvalidOperationException) { Assert(true, message); return; }
        throw new Exception(message);
    }

    private static void Assert(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
}
