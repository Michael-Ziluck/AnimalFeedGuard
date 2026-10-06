using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AnimalFeedGuard;
using Mono.Cecil;
using Mono.Cecil.Cil;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    checks++;
}
Check(FeedRules.WithinRadius(25, 5), "exact radius is included");
Check(!FeedRules.WithinRadius(25.01f, 5), "outside radius is excluded");
Check(FeedRules.WithinRadius(0, 5), "same position is protected");
Check(!FeedRules.WithinRadius(0, 0), "zero radius disables protection");
Check(!FeedRules.WithinRadius(0, -1), "negative radius cannot protect");
Check(!FeedRules.WithinRadius(float.NaN, 5), "invalid position cannot protect");
Check(!FeedRules.WithinRadius(9 + 16 + 1, 5), "height counts in radius");
Check(FeedRules.SameFood("$item_barley", "$item_barley"), "animal-only feed matches");
Check(FeedRules.SameFood("$mod_customfeed", "$mod_customfeed"), "modded food identity matches");
Check(!FeedRules.SameFood("$item_carrot", "$item_barley"), "wrong animal diet does not match");
Check(!FeedRules.SameFood(null, null), "missing food is not protected");
Check(!FeedRules.SameFood("", ""), "empty food is not protected");
Check(!FeedRules.SameFood("$item_barley", "$ITEM_BARLEY"), "food identity is case sensitive");
Check(FeedRules.Protects(true, true, 0, 5, "$item_barley", new string?[] { null, "", "$item_barley" }), "missing diet entries do not hide matching feed");
int dietVisits = 0;
IEnumerable<string?> ObservedDiet()
{
    dietVisits++;
    yield return "$item_barley";
    throw new Exception("diet was enumerated after a match");
}
Check(!FeedRules.Protects(false, true, 0, 5, "$item_barley", ObservedDiet()) && dietVisits == 0, "dead animals do not enumerate their diet");
Check(!FeedRules.Protects(true, false, 0, 5, "$item_barley", ObservedDiet()) && dietVisits == 0, "wild animals do not enumerate their diet");
Check(!FeedRules.Protects(true, true, 26, 5, "$item_barley", ObservedDiet()) && dietVisits == 0, "distant animals do not enumerate their diet");
Check(FeedRules.Protects(true, true, 0, 5, "$item_barley", ObservedDiet()) && dietVisits == 1, "diet matching stops at the first match");

// This matrix was extracted from the installed 1.0.16 prefab assets, rather
// than guessed from player-food categories or a species whitelist.
using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "creature-foods.json")));
var creatures = fixture.RootElement.GetProperty("creatures").EnumerateArray().ToArray();
var allFoods = creatures.SelectMany(c => c.GetProperty("foods").EnumerateArray())
    .Select(f => f.GetProperty("sharedName").GetString()!).Distinct().ToArray();
int dietPairs = 0;
foreach (var creature in creatures)
{
    string name = creature.GetProperty("creature").GetString()!;
    string[] diet = creature.GetProperty("foods").EnumerateArray().Select(f => f.GetProperty("sharedName").GetString()!).ToArray();
    foreach (string food in diet)
    {
        dietPairs++;
        Check(FeedRules.Protects(true, true, 0, 5, food, diet), $"{name} protects {food} beside its body");
        Check(FeedRules.Protects(true, true, 25, 5, food, diet), $"{name} protects {food} at the radius boundary");
        Check(!FeedRules.Protects(true, true, 25.01f, 5, food, diet), $"{name} allows {food} outside the radius");
        Check(!FeedRules.Protects(true, false, 0, 5, food, diet), $"wild {name} does not protect {food}");
        Check(!FeedRules.Protects(false, true, 0, 5, food, diet), $"dead {name} does not protect {food}");
        Check(!FeedRules.Protects(true, true, 0, 0, food, diet), $"zero radius disables protection for {name}");
    }
    foreach (string food in allFoods)
        Check(FeedRules.Protects(true, true, 0, 5, food, diet) == diet.Contains(food), $"{name} matches its diet exactly for {food}");
}
Check(dietPairs == 48, "all 48 audited creature/food combinations are covered");
Check(creatures.Any(c => c.GetProperty("creature").GetString() == "Moose"), "Deep North moose coverage is present");
Check(creatures.Any(c => c.GetProperty("creature").GetString() == "Bjorn_spiritcaller"), "Deep North summoned bear coverage is present");
Check(FeedRules.Protects(true, true, 0, 5, "$item_smokepuff", new[] { "$item_vineberry", "$item_fiddleheadfern", "$item_smokepuff" }), "Asksvin protects Smokepuffs");
Check(FeedRules.Protects(true, true, 0, 5, "$mod_customfeed", new[] { "$mod_customfeed" }), "runtime modded diet additions are protected");
Check(!FeedRules.Protects(true, true, 0, 5, "$item_smokepuff", new[] { "$item_lingonberries" }), "diet replacement removes old protection");
Check(!FeedRules.Protects(true, true, 0, 5, "$item_smokepuff", Array.Empty<string>()), "empty diet cannot protect food");

if (args.Length < 1 || args.Length > 2) throw new ArgumentException("Supply the game path and optional installed AutoPicker DLL path.");
using var game = AssemblyDefinition.ReadAssembly(Path.Combine(args[0], "valheim_Data", "Managed", "assembly_valheim.dll"));
var player = game.MainModule.Types.Single(t => t.Name == "Player");
var pickup = player.Methods.Single(m => m.Name == "AutoPickup");
var code = pickup.Body.Instructions;
var guards = code.Where(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f && f.DeclaringType.Name == "ItemDrop" && f.Name == "m_autoPickup").ToList();
Check(guards.Count == 1, "installed game has exactly one replaceable pickup flag check");
int guard = code.IndexOf(guards[0]);
Check(code[guard + 1].OpCode.FlowControl == FlowControl.Cond_Branch, "guard controls pickup branch");
int move = code.ToList().FindIndex(i => i.Operand is MethodReference m && m.Name == "set_position");
int collect = code.ToList().FindIndex(i => i.Operand is MethodReference m && m.Name == "Pickup");
Check(move > guard && collect > guard, "filter runs before item movement and collection");
var branch = code.IndexOf((Instruction)code[guard + 1].Operand);
Check(branch > move && branch > collect, "rejected item skips movement and collection");
var ai = game.MainModule.Types.Single(t => t.Name == "MonsterAI");
Check(ai.Fields.Any(f => f.Name == "m_consumeItems" && f.IsPublic), "diet list is available without publicizing assemblies");
Check(ai.Methods.Single(m => m.Name == "CanConsume").Body.Instructions.Any(i => i.Operand is FieldReference f && f.Name == "m_name"), "vanilla feeding compares shared food names");

if (args.Length == 2)
{
using var autoPicker = AssemblyDefinition.ReadAssembly(args[1]);
var checkAndPick = autoPicker.MainModule.Types.Single(t => t.FullName == "AutoPicker.AutoPicker").Methods.Single(m => m.Name == "CheckAndPick");
Check(checkAndPick.Body.Instructions.Any(i => i.Operand is MethodReference m && m.DeclaringType.Name == "Pickable" && m.Name == "Interact"), "installed AutoPicker harvests Pickables");
Check(!checkAndPick.Body.Instructions.Any(i => i.Operand is MethodReference m && (m.Name == "AddItem" || m.Name == "Pickup")), "AutoPicker loop does not bypass ground pickup to collect inventory items");
}
Console.WriteLine($"PASS: {checks} feed-rule and installed-code compatibility checks, covering {dietPairs} food combinations across {creatures.Length} creature diets. Gameplay not exercised.");
