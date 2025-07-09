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

    [JsonPropertyName("CommandBurstLimit")]
    public int CommandBurstLimit { get; set; } = 3;
}

[MinimumApiVersion(130)]
public class AntiSpamPlugin : BasePlugin, IPluginConfig<AntiSpamConfig>
{
    public override string ModuleName => "AntiSpamChat";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "GSM-RO";
    public override string ModuleDescription => "Anti-spam cu limită de execuție și cooldown";

    public AntiSpamConfig Config { get; set; } = new();

    // Număr de comenzi executate fără cooldown
    private readonly Dictionary<ulong, int> burstCount = new();
    private readonly Dictionary<ulong, float> cooldownStartTime = new();

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
            string command = text[1..].Split(' ')[0];
            if (Config.ProtectedCommands.Contains(command))
                return HandleCommand(player, command);
        }

        return HookResult.Continue;
    }

    private HookResult HandleCommand(CCSPlayerController player, string commandName)
    {
        ulong steamId = player.AuthorizedSteamID!.SteamId64;
        float currentTime = (float)Server.EngineTime;

        // Verificăm dacă jucătorul este în cooldown
        if (cooldownStartTime.TryGetValue(steamId, out float cooldownStarted))
        {
            if (currentTime - cooldownStarted < Config.CooldownSeconds)
            {
                player.PrintToChat("\x02[AntiSpam] \x06Trebuie să aștepți " + Config.CooldownSeconds + " secunde după " + Config.CommandBurstLimit + " comenzi protejate!");
                return HookResult.Handled;
            }
            else
            {
                // Cooldown-ul a expirat
                cooldownStartTime.Remove(steamId);
                burstCount[steamId] = 0;
            }
        }

        // Actualizăm numărul de comenzi executate
        if (!burstCount.ContainsKey(steamId))
            burstCount[steamId] = 0;

        burstCount[steamId]++;

        if (burstCount[steamId] >= Config.CommandBurstLimit)
        {
            cooldownStartTime[steamId] = currentTime;
            player.PrintToChat("\x02[AntiSpam] \x06Ai atins limita de comenzi! Cooldown activ pentru " + Config.CooldownSeconds + " secunde.");
        }

        return HookResult.Continue;
    }
}