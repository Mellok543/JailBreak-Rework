using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak;

public class CtAccessService : IFeature
{
    private readonly JailBreak _jailBreak;

    private readonly Dictionary<ulong, DateTime> _ctBlockedUntil = new();
    private readonly Dictionary<ulong, CtTestSession> _activeTests = new();
    private readonly HashSet<ulong> _queuedForCt = new();
    private readonly HashSet<ulong> _passedThisRound = new();
    private readonly HashSet<ulong> _ctSwitchAuthorized = new();

    private readonly string _configPath;
    private readonly string _dataPath;

    private CtAccessConfig _config = new();

    public CtAccessService(JailBreak jailBreak)
    {
        _jailBreak = jailBreak;

        var basePath = ResolveStorageDirectory();
        _configPath = Path.Combine(basePath, "ct_access_config.json");
        _dataPath = Path.Combine(basePath, "ct_access_state.json");

        LoadConfig();
        LoadState();

        jailBreak.RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        jailBreak.RegisterEventHandler<EventRoundEnd>(OnRoundEnd, HookMode.Post);
        jailBreak.RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam, HookMode.Post);
        jailBreak.RegisterListener<Listeners.OnClientDisconnect>(OnClientDisconnect);
        jailBreak.RegisterListener<Listeners.OnClientPutInServer>(OnClientPutInServer);

        jailBreak.AddCommandListener("jointeam", OnJoinTeamCommand);
        jailBreak.AddCommandListener("spectate", OnSpectateCommand);
    }

    private string ResolveStorageDirectory()
    {
        var candidates = new[]
        {
            _jailBreak.ModuleDirectory,
            Path.GetDirectoryName(typeof(CtAccessService).Assembly.Location),
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        var path = candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        if (path is null)
            throw new InvalidOperationException("Unable to resolve storage path for CtAccessService");

        Directory.CreateDirectory(path);
        return path;
    }


    private void OnClientPutInServer(int slot)
    {
        var player = Utilities.GetPlayerFromSlot(slot);
        if (!player.IsLegal())
            return;

        _jailBreak.AddTimer(0.2f, () =>
        {
            if (!player.IsLegal())
                return;

            if (player.Team == CsTeam.None)
                MoveToTeam(player, CsTeam.Terrorist);
        });
    }

    private HookResult OnJoinTeamCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!player.IsLegal())
            return HookResult.Continue;

        var arg = commandInfo.ArgByIndex(1);
        if (string.IsNullOrWhiteSpace(arg) || !int.TryParse(arg, out var teamNum))
            return HookResult.Continue;

        if (teamNum == 0)
        {
            player.PrintToChat("Случайный выбор команды отключён. Выберите Т или наблюдателей.");
            return HookResult.Handled;
        }

        if (teamNum == (int)CsTeam.CounterTerrorist)
        {
            if (_ctSwitchAuthorized.Remove(player.SteamID))
                return HookResult.Continue;

            player.PrintToChat("Переход за КТ только через !ct");
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    private HookResult OnSpectateCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (!player.IsLegal())
            return HookResult.Continue;

        _activeTests.Remove(player.SteamID);
        _queuedForCt.Remove(player.SteamID);
        _passedThisRound.Remove(player.SteamID);

        return HookResult.Continue;
    }

    public void HandleCtCommand(CCSPlayerController? player)
    {
        if (!player.IsLegal())
            return;

        if (player.Team == CsTeam.CounterTerrorist)
        {
            player.PrintToChat("Вы уже в команде КТ");
            return;
        }

        if (player.Team == CsTeam.Spectator)
        {
            player.PrintToChat("Наблюдатели разрешены. Для теста перейдите в команду Т");
            return;
        }

        var steamId = player.SteamID;

        if (IsBlocked(steamId, out var secondsLeft))
        {
            player.PrintToChat($"Команда КТ заблокирована ещё на {secondsLeft} сек.");
            return;
        }

        if (_activeTests.ContainsKey(steamId))
        {
            player.PrintToChat("Тест уже запущен");
            return;
        }

        if (_config.Questions.Count == 0)
        {
            player.PrintToChat("Тест недоступен: нет вопросов в конфиге");
            return;
        }

        StartTest(player);
    }

    private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (!player.IsLegal())
            return HookResult.Continue;

        var steamId = player.SteamID;

        if (player.Team == CsTeam.CounterTerrorist)
        {
            if (_ctSwitchAuthorized.Remove(steamId))
                return HookResult.Continue;

            if (!_queuedForCt.Contains(steamId) && !_passedThisRound.Contains(steamId))
            {
                player.PrintToChat("Переход за КТ только через !ct");
                _jailBreak.AddTimer(0.1f, () => MoveToTeam(player, CsTeam.Terrorist));
            }
        }

        if (player.Team == CsTeam.Terrorist)
        {
            _passedThisRound.Remove(steamId);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        CleanupExpiredBlocks();

        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsLegal()) continue;
            if (player.Team == CsTeam.None)
            {
                MoveToTeam(player, CsTeam.Terrorist);
            }
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        PromoteQueuedPlayers();
        return HookResult.Continue;
    }

    private void StartTest(CCSPlayerController player)
    {
        var questionCount = Math.Clamp(_config.QuestionsToAsk, 1, _config.Questions.Count);
        var selectedQuestions = _config.Questions.OrderBy(_ => Random.Shared.Next()).Take(questionCount)
            .Select(q => q.Clone()).ToList();

        var session = new CtTestSession
        {
            Player = player,
            StartedAt = DateTime.UtcNow,
            Questions = selectedQuestions,
            CurrentQuestionIndex = 0
        };

        _activeTests[player.SteamID] = session;

        _jailBreak.AddTimer(_config.TimeLimitSeconds, () =>
        {
            if (_activeTests.TryGetValue(player.SteamID, out var active) && active.StartedAt == session.StartedAt)
            {
                FailTest(player, "Время вышло");
            }
        });

        player.PrintToChat($"Тест за КТ начат. Время: {_config.TimeLimitSeconds} сек.");

        ShowQuestion(session);
    }

    private void ShowQuestion(CtTestSession session)
    {
        if (!session.Player.IsLegal())
        {
            _activeTests.Remove(session.SteamId);
            return;
        }

        if ((DateTime.UtcNow - session.StartedAt).TotalSeconds > _config.TimeLimitSeconds)
        {
            FailTest(session.Player, "Время вышло");
            return;
        }

        if (session.CurrentQuestionIndex >= session.Questions.Count)
        {
            PassTest(session.Player);
            return;
        }

        var question = session.Questions[session.CurrentQuestionIndex];
        var options = question.Options.OrderBy(_ => Random.Shared.Next()).ToList();

        var menu = new ChatMenu($"Тест КТ {session.CurrentQuestionIndex + 1}/{session.Questions.Count}: {question.Question}");
        foreach (var option in options)
        {
            menu.AddMenuOption(option.Text, (controller, _) =>
            {
                if (!_activeTests.TryGetValue(controller.SteamID, out var activeSession))
                    return;

                if ((DateTime.UtcNow - activeSession.StartedAt).TotalSeconds > _config.TimeLimitSeconds)
                {
                    FailTest(controller, "Время вышло");
                    return;
                }

                if (!option.IsCorrect)
                {
                    FailTest(controller, "Неверный ответ");
                    return;
                }

                activeSession.CurrentQuestionIndex++;
                ShowQuestion(activeSession);
            });
        }

        MenuManager.OpenChatMenu(session.Player, menu);
    }

    private void PassTest(CCSPlayerController player)
    {
        _activeTests.Remove(player.SteamID);

        _passedThisRound.Add(player.SteamID);

        if (CanJoinCtNow())
        {
            _queuedForCt.Add(player.SteamID);
            player.PrintToChat("Тест пройден! Вы будете переведены в КТ в конце раунда.");
        }
        else
        {
            _queuedForCt.Add(player.SteamID);
            player.PrintToChat("Тест пройден! Вы добавлены в очередь на КТ.");
        }
    }

    private void FailTest(CCSPlayerController player, string reason)
    {
        _activeTests.Remove(player.SteamID);

        var until = DateTime.UtcNow.AddMinutes(_config.BlockMinutes);
        _ctBlockedUntil[player.SteamID] = until;
        SaveState();

        player.PrintToChat($"Тест не пройден: {reason}. КТ заблокирована на {_config.BlockMinutes} минут.");
    }

    private void PromoteQueuedPlayers()
    {
        CleanupExpiredBlocks();

        if (_queuedForCt.Count == 0)
            return;

        var queuePlayers = Utilities.GetPlayers()
            .Where(p => p.IsLegal() && _queuedForCt.Contains(p.SteamID) && p.Team != CsTeam.CounterTerrorist)
                        .OrderBy(p => p.Slot)
            .ToList();

        foreach (var player in queuePlayers)
        {
            if (!CanJoinCtNow())
                break;

            _queuedForCt.Remove(player.SteamID);
            _passedThisRound.Remove(player.SteamID);
            _ctSwitchAuthorized.Add(player.SteamID);
            MoveToTeam(player, CsTeam.CounterTerrorist);
            player.PrintToChat("Вы переведены в команду КТ");
        }
    }

    private bool CanJoinCtNow()
    {
        var players = Utilities.GetPlayers().Where(p => p.IsLegal() && p.Team != CsTeam.Spectator).ToList();
        var total = players.Count;
        var ct = players.Count(p => p.Team == CsTeam.CounterTerrorist);

        if (total == 0)
            return false;

        var maxCt = (int)Math.Ceiling(total / 3.0);
        return ct < Math.Max(1, maxCt);
    }

    private static void MoveToTeam(CCSPlayerController player, CsTeam team)
    {
        if (!player.IsLegal())
            return;

        player.SwitchTeam(team);
    }

    private bool IsBlocked(ulong steamId, out int secondsLeft)
    {
        secondsLeft = 0;

        if (!_ctBlockedUntil.TryGetValue(steamId, out var until))
            return false;

        if (until <= DateTime.UtcNow)
        {
            _ctBlockedUntil.Remove(steamId);
            SaveState();
            return false;
        }

        secondsLeft = (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds);
        return true;
    }

    private void CleanupExpiredBlocks()
    {
        var changed = false;
        foreach (var key in _ctBlockedUntil.Keys.ToList())
        {
            if (_ctBlockedUntil[key] <= DateTime.UtcNow)
            {
                _ctBlockedUntil.Remove(key);
                changed = true;
            }
        }

        if (changed)
            SaveState();
    }

    private void OnClientDisconnect(int slot)
    {
        var player = Utilities.GetPlayers().FirstOrDefault(p => p.Slot == slot);
        if (player == null)
            return;

        _activeTests.Remove(player.SteamID);
    }

    private void LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            _config = CtAccessConfig.CreateDefault();
            File.WriteAllText(_configPath, JsonSerializer.Serialize(_config, JsonOptions()));
            return;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<CtAccessConfig>(File.ReadAllText(_configPath), JsonOptions());
            _config = loaded ?? CtAccessConfig.CreateDefault();
        }
        catch
        {
            _config = CtAccessConfig.CreateDefault();
        }
    }

    private void LoadState()
    {
        if (!File.Exists(_dataPath))
            return;

        try
        {
            var state = JsonSerializer.Deserialize<CtAccessState>(File.ReadAllText(_dataPath), JsonOptions());
            if (state?.BlockedUntil == null)
                return;

            foreach (var (key, value) in state.BlockedUntil)
            {
                if (ulong.TryParse(key, out var steamId))
                    _ctBlockedUntil[steamId] = value;
            }
        }
        catch
        {
            // ignore broken state file
        }
    }

    private void SaveState()
    {
        var state = new CtAccessState
        {
            BlockedUntil = _ctBlockedUntil.ToDictionary(k => k.Key.ToString(), v => v.Value)
        };

        File.WriteAllText(_dataPath, JsonSerializer.Serialize(state, JsonOptions()));
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }
}

public class CtTestSession
{
    public required CCSPlayerController Player { get; init; }
    public ulong SteamId => Player.SteamID;
    public required List<CtQuestion> Questions { get; init; }
    public required DateTime StartedAt { get; init; }
    public int CurrentQuestionIndex { get; set; }
}

public class CtAccessState
{
    public Dictionary<string, DateTime> BlockedUntil { get; set; } = new();
}

public class CtAccessConfig
{
    public int TimeLimitSeconds { get; set; } = 60;
    public int BlockMinutes { get; set; } = 5;
    public int QuestionsToAsk { get; set; } = 3;
    public List<CtQuestion> Questions { get; set; } = new();

    public static CtAccessConfig CreateDefault()
    {
        return new CtAccessConfig
        {
            Questions =
            [
                new CtQuestion
                {
                    Question = "Можно ли убивать без причины?",
                    Options =
                    [
                        new CtAnswerOption { Text = "Нет", IsCorrect = true },
                        new CtAnswerOption { Text = "Да", IsCorrect = false },
                        new CtAnswerOption { Text = "Только по выходным", IsCorrect = false }
                    ]
                },
                new CtQuestion
                {
                    Question = "Что делать с заключёнными в начале раунда?",
                    Options =
                    [
                        new CtAnswerOption { Text = "Дать приказ", IsCorrect = true },
                        new CtAnswerOption { Text = "Сразу расстрелять", IsCorrect = false },
                        new CtAnswerOption { Text = "Игнорировать", IsCorrect = false }
                    ]
                },
                new CtQuestion
                {
                    Question = "Кому можно стать командиром?",
                    Options =
                    [
                        new CtAnswerOption { Text = "Только КТ", IsCorrect = true },
                        new CtAnswerOption { Text = "Любому игроку", IsCorrect = false },
                        new CtAnswerOption { Text = "Только наблюдателю", IsCorrect = false }
                    ]
                }
            ]
        };
    }
}

public class CtQuestion
{
    public string Question { get; set; } = string.Empty;
    public List<CtAnswerOption> Options { get; set; } = new();

    public CtQuestion Clone()
    {
        return new CtQuestion
        {
            Question = Question,
            Options = Options.Select(option => new CtAnswerOption
            {
                Text = option.Text,
                IsCorrect = option.IsCorrect
            }).ToList()
        };
    }
}

public class CtAnswerOption
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
