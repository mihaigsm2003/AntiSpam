using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Events;

namespace AntiSpamPlugin;

public class AntiSpamConfig : BasePluginConfig
{
    [JsonPropertyName("CooldownSeconds")]
    public float CooldownSeconds { get; set; } = 10.0f;

    [JsonPropertyName("ProtectedCommands")]
    public List<string> ProtectedCommands { get; set; } = new() { "css_heal", "css_give", "css_vip", "css_say" };
}

[MinimumApiVersion(130)]
public class AntiSpamPlugin : BasePlugin, IPluginConfig<AntiSpamConfig>
{
    public override string ModuleName => "AntiSpamChat";
    public override string ModuleVersion => "1.0.2";
    public override string ModuleAuthor => "GSM-RO";
    public override string ModuleDescription => "Anti-spam for console commands and chat";

    public AntiSpamConfig Config { get; set; } = new();

    private readonly Dictionary<ulong, float> lastCommandTime = new();

    public void OnConfigParsed(AntiSpamConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerChat>(OnEventPlayerChat);

        foreach (var command in Config.ProtectedCommands)
        {
            RegisterCssCommand(command);
        }
    }

    private void RegisterCssCommand(string commandName)
    {
        AddCommandListener(commandName, (player, info) =>
        {
            if (player == null || player.AuthorizedSteamID == null)
                return HookResult.Continue;

            return HandleCommand(player, commandName);
        });
    }

    private HookResult OnEventPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid || @event.Text == null || player.AuthorizedSteamID == null)
            return HookResult.Continue;

        string text = @event.Text.Trim();

        if (text.StartsWith("!") || text.StartsWith("/"))
        {
            string command = text[1..].Split(' ')[0]; // Get the command part after the prefix
            if (Config.ProtectedCommands.Contains(command))
                return HandleCommand(player, command);
        }

        return HookResult.Continue;
    }

    private HookResult HandleCommand(CCSPlayerController player, string commandName)
    {
        if (player.AuthorizedSteamID == null)
            return HookResult.Continue;

        ulong steamId = player.AuthorizedSteamID.SteamId64;
        float currentTime = (float)Server.EngineTime;

        if (lastCommandTime.TryGetValue(steamId, out float lastTime))
        {
            if (currentTime - lastTime < Config.CooldownSeconds)
            {
                player.PrintToChat("\x02[AntiSpam] \x01***********************************");
                player.PrintToChat($"\x02[AntiSpam] \x06 Wait \x02{Config.CooldownSeconds} seconds\x06 between commands!");
                player.PrintToChat("\x02[AntiSpam] \x01***********************************");
                return HookResult.Handled;
            }
        }

        lastCommandTime[steamId] = currentTime;
        return HookResult.Continue;
    }
}
