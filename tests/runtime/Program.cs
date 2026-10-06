using System;
using System.Linq;
using System.Reflection;
using AnimalFeedGuard;
using BepInEx.Configuration;
using UnityEngine;

internal static class Program
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks++;
    }

    private static ItemDrop Feed(string name)
    {
        var drop = new ItemDrop();
        drop.m_itemData.m_shared.m_name = name;
        return drop;
    }

    private static void Main()
    {
        var plugin = new Plugin();
        typeof(Plugin).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plugin, null);
        var enabled = (ConfigEntry<bool>)plugin.Config.Entries["Enabled"];
        var radius = (ConfigEntry<float>)plugin.Config.Entries["Protection Radius"];
        var diagnostics = (ConfigEntry<bool>)plugin.Config.Entries["Log Pickup Decisions"];
        Check(radius.Value == 25 && enabled.Value && !diagnostics.Value, "current defaults");

        var feed = Feed("$item_smokepuff");
        var animal = new Character { Tamed = true };
        animal.gameObject.name = "Asksvin";
        var ai = new MonsterAI();
        ai.m_consumeItems.Add(Feed("$item_smokepuff"));
        animal.Components[typeof(MonsterAI)] = ai;
        Character.Characters.Add(animal);
        Time.frameCount = 1;
        Check(!Plugin.CanAutoPickup(feed), "living tame eater protects matching feed");
        Check(feed.m_autoPickup, "filter leaves item eligibility unchanged");
        Check(Plugin.CanAutoPickup(Feed("$item_barley")), "wrong diet remains collectible");
        Check(Plugin.CanAutoPickup(Feed("")), "missing food name remains collectible");
        Check(!Plugin.CanAutoPickup(null!), "missing drop cannot be collected");
        feed.m_autoPickup = false;
        Check(!Plugin.CanAutoPickup(feed), "original eligibility is respected");
        feed.m_autoPickup = true;

        animal.Dead = true;
        Check(Plugin.CanAutoPickup(feed), "death is observed within the cached frame");
        animal.Dead = false; animal.Tamed = false;
        Check(Plugin.CanAutoPickup(feed), "wild animals do not protect feed");
        var view = new ZNetView();
        animal.Components[typeof(ZNetView)] = view;
        view.Record.Tamed = true;
        Check(!Plugin.CanAutoPickup(feed), "synchronized tame flag overrides stale local state");
        view.Record.Tamed = false; animal.Tamed = true;
        Check(Plugin.CanAutoPickup(feed), "synchronized wild flag overrides stale local state");
        view.Valid = false;
        Check(!Plugin.CanAutoPickup(feed), "invalid network view uses local tameness");
        view.Valid = true; view.Record.Tamed = null;
        Check(!Plugin.CanAutoPickup(feed), "missing synchronized flag uses local tameness");

        animal.transform.position = new Vector3(25, 0, 0);
        Check(!Plugin.CanAutoPickup(feed), "exact radius protects feed");
        animal.transform.position = new Vector3(25.01f, 0, 0);
        Check(Plugin.CanAutoPickup(feed), "position changes are observed within the cached frame");
        animal.Body = new Collider { Contact = new Vector3(24, 0, 0) };
        Check(!Plugin.CanAutoPickup(feed), "active body contact is used instead of origin");
        animal.Body.enabled = false;
        Check(Plugin.CanAutoPickup(feed), "disabled body falls back to origin");
        animal.Body.enabled = true; animal.Body.gameObject.activeInHierarchy = false;
        Check(Plugin.CanAutoPickup(feed), "inactive body falls back to origin");
        animal.Body = null; animal.transform.position = new Vector3(0, 0, 0);
        ai.m_consumeItems.Clear();
        Check(Plugin.CanAutoPickup(feed), "diet changes are observed within the cached frame");
        ai.m_consumeItems.Add(Feed("$item_smokepuff"));
        ai.m_consumeItems.Add(null!);
        Check(!Plugin.CanAutoPickup(feed), "matching diet is restored immediately");

        enabled.Value = false;
        Check(Plugin.CanAutoPickup(feed), "disabled protection allows original eligible items");
        enabled.Value = true; radius.Value = 0;
        Check(Plugin.CanAutoPickup(feed), "zero radius disables protection");
        radius.Value = 25;
        diagnostics.Value = true;
        Time.timeAsDouble = 100;
        Check(!Plugin.CanAutoPickup(feed), "diagnostics do not change protection");
        Check(plugin.Logger.Messages.Last().Contains("blocked") && plugin.Logger.Messages.Last().Contains("Asksvin"), "blocked decision identifies the matching creature");
        int logCount = plugin.Logger.Messages.Count;
        Plugin.CanAutoPickup(feed);
        Check(plugin.Logger.Messages.Count == logCount, "repeated outcome is rate limited");
        Time.timeAsDouble = 105;
        Plugin.CanAutoPickup(feed);
        Check(plugin.Logger.Messages.Count == logCount + 1, "decision logs resume at five seconds");
        animal.Tamed = false;
        Check(Plugin.CanAutoPickup(feed) && plugin.Logger.Messages.Last().Contains("allowed"), "allowed outcome has its own rate limit");
        Check(plugin.Logger.Messages.Last().Contains("tamed=False"), "diagnostics describe the matching wild animal");
        animal.Tamed = true;

        var player = new Player();
        player.Drops.Add(feed);
        player.AutoPickup(.02f);
        Check(!feed.Collected, "Harmony filter blocks automatic collection");
        enabled.Value = false;
        player.AutoPickup(.02f);
        Check(feed.Collected, "disabled protection preserves automatic collection");
        enabled.Value = true;
        typeof(Plugin).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(plugin, null);
        feed.Collected = false;
        player.AutoPickup(.02f);
        Check(feed.Collected, "plugin destruction removes the filter");
        Console.WriteLine($"PASS: {checks} filter and Harmony checks against simulated game objects. Unity physics and gameplay are not exercised.");
    }
}
