
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private readonly Dictionary<string, EspRoleSettings>
            _espRolesByKey =
                new Dictionary<string, EspRoleSettings>(
                    StringComparer.OrdinalIgnoreCase);
        private readonly List<EspRoleSettings> _espRoles =
            new List<EspRoleSettings>(64);
        private readonly List<EspRoleGroup> _espRoleGroups =
            new List<EspRoleGroup>(8);
        private readonly Dictionary<string, EspRoleGroup>
            _espRoleGroupsByName =
                new Dictionary<string, EspRoleGroup>(
                    StringComparer.OrdinalIgnoreCase);
        private bool _espAllRolesExpanded;
        private bool _chamAllRolesExpanded;

        private void ConfigureRoleEsp()
        {
            AddRoleEsp("PMC-BEAR", "PMC - BEAR", EspKind.Pmc, "BEAR");
            AddRoleEsp("PMC-USEC", "PMC - USEC", EspKind.Pmc, "USEC");
            AddRoleEsp(
                "AI-BOSS",
                "Boss - Runtime Boss",
                EspKind.Boss,
                "Runtime Boss");
            AddRoleEsp(
                "AI-FOLLOWER",
                "Follower - Runtime Follower",
                EspKind.Boss,
                "Runtime Follower");

            Array roles = Enum.GetValues(typeof(WildSpawnType));
            for (int i = 0; i < roles.Length; i++)
            {
                WildSpawnType role = (WildSpawnType)roles.GetValue(i);
                EspKind kind = RoleKind(role);
                string roleName = role.ToString();
                AddRoleEsp(
                    "ROLE-" + roleName,
                    RoleGroupName(role) + " - " + roleName,
                    kind,
                    roleName);
            }
        }

        private void AddRoleEsp(
            string key,
            string label,
            EspKind kind,
            string configKey)
        {
            Color visible = GetRoleDefaultColor(key, kind);
            Color hidden = new Color(
                visible.r * 0.3f,
                visible.g * 0.3f,
                visible.b * 0.3f,
                0.75f);
            EspRoleSettings settings = new EspRoleSettings
            {
                Key = key,
                Label = label,
                Group = label.Substring(0, label.IndexOf(" - ",
                    StringComparison.Ordinal)),
                Kind = kind,
                DefaultVisible = visible,
                DefaultHidden = hidden,
                Enabled = Config.Bind(
                    "ESP Roles",
                    configKey + " Enabled",
                    true,
                    "Render " + label + " targets."),
                VisibleColor = Config.Bind(
                    "ESP Role Colors",
                    configKey + " Visible",
                    "#" + ColorUtility.ToHtmlStringRGBA(visible),
                    "Visible ESP color for " + label + "."),
                HiddenColor = Config.Bind(
                    "ESP Role Colors",
                    configKey + " Hidden",
                    "#" + ColorUtility.ToHtmlStringRGBA(hidden),
                    "Occluded ESP color for " + label + "."),
                ChamsEnabled = Config.Bind(
                    "Cham Roles",
                    configKey + " Enabled",
                    true,
                    "Apply character chams to " + label + "."),
                ChamVisibleColor = Config.Bind(
                    "Cham Role Colors",
                    configKey + " Visible",
                    "#" + ColorUtility.ToHtmlStringRGBA(visible),
                    "Visible cham color for " + label + "."),
                ChamHiddenColor = Config.Bind(
                    "Cham Role Colors",
                    configKey + " Hidden",
                    "#" + ColorUtility.ToHtmlStringRGBA(hidden),
                    "Occluded cham color for " + label + ".")
            };
            _espRoles.Add(settings);
            _espRolesByKey.Add(key, settings);

            EspRoleGroup group;
            if (!_espRoleGroupsByName.TryGetValue(
                    settings.Group, out group))
            {
                group = new EspRoleGroup
                {
                    Name = settings.Group
                };
                _espRoleGroupsByName.Add(settings.Group, group);
                _espRoleGroups.Add(group);
            }
            group.Roles.Add(settings);
        }

        private static EspKind RoleKind(WildSpawnType role)
        {
            return IsOrdinaryScavRole(role)
                ? EspKind.Scav
                : EspKind.Boss;
        }

        private static string RoleGroupName(WildSpawnType role)
        {
            string name = role.ToString();
            if (IsOrdinaryScavRole(role))
                return "Scav";
            if (name.StartsWith("boss", StringComparison.OrdinalIgnoreCase))
                return "Boss";
            if (name.StartsWith("follower", StringComparison.OrdinalIgnoreCase))
                return "Follower";
            if (name.StartsWith("sect", StringComparison.OrdinalIgnoreCase))
                return "Cultist";
            if (name.StartsWith("infected", StringComparison.OrdinalIgnoreCase))
                return "Infected";
            if (name.IndexOf("pmc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                role == WildSpawnType.exUsec)
                return "Raider / Rogue";
            return "Special";
        }

        private static Color GetRoleDefaultColor(string key, EspKind kind)
        {
            Color baseColor = GetVisualFallback(kind);
            int hash = 17;
            for (int i = 0; i < key.Length; i++)
                hash = unchecked(hash * 31 + key[i]);
            float shift = ((hash & 255) / 255f - 0.5f) * 0.16f;
            return new Color(
                Mathf.Clamp01(baseColor.r + shift),
                Mathf.Clamp01(baseColor.g - shift * 0.5f),
                Mathf.Clamp01(baseColor.b + shift * 0.35f),
                1f);
        }

        private string GetRoleKey(Player player)
        {
            try
            {
                if (player.AIData != null &&
                    player.AIData.IsAI)
                {
                    WildSpawnType role =
                        player.Profile.Info.Settings.Role;
                    string roleName = role.ToString();
                    BotOwner owner = player.AIData.BotOwner;
                    if (IsRuntimeBoss(owner) &&
                        roleName.StartsWith(
                            "follower",
                            StringComparison.OrdinalIgnoreCase))
                        return "AI-BOSS";
                    if (IsRuntimeFollower(owner) &&
                        !roleName.StartsWith(
                            "follower",
                            StringComparison.OrdinalIgnoreCase))
                        return "AI-FOLLOWER";
                    return "ROLE-" + roleName;
                }
            }
            catch { }
            if (player.Side == EPlayerSide.Bear)
                return "PMC-BEAR";
            if (player.Side == EPlayerSide.Usec)
                return "PMC-USEC";
            try
            {
                return "ROLE-" + player.Profile.Info.Settings.Role;
            }
            catch { return "ROLE-assault"; }
        }

        private static bool IsRuntimeBoss(BotOwner owner)
        {
            return owner != null &&
                   owner.Boss != null &&
                   owner.Boss.IamBoss;
        }

        private static bool IsRuntimeFollower(BotOwner owner)
        {
            if (owner == null || IsRuntimeBoss(owner))
                return false;
            try
            {
                return owner.IsFollower() ||
                       (owner.BotFollower != null &&
                        owner.BotFollower.HaveBoss);
            }
            catch
            {
                return false;
            }
        }

        private EspRoleSettings GetRoleSettings(string key)
        {
            EspRoleSettings settings;
            return key != null && _espRolesByKey.TryGetValue(key, out settings)
                ? settings
                : null;
        }

        private bool ShouldShow(Target target)
        {
            EspRoleSettings settings = GetRoleSettings(target.RoleKey);
            return settings != null
                ? settings.Enabled.Value
                : ShouldShow(target.Kind);
        }

        private Color GetRoleColor(Target target, bool hidden)
        {
            EspRoleSettings settings = GetRoleSettings(target.RoleKey);
            if (settings == null)
                return GetVisualColor(target.Kind, hidden);
            return ParseVisualColor(
                hidden
                    ? settings.HiddenColor.Value
                    : settings.VisibleColor.Value,
                hidden
                    ? settings.DefaultHidden
                    : settings.DefaultVisible);
        }

        private bool ShouldShowRoleChams(Target target)
        {
            EspRoleSettings settings = GetRoleSettings(target.RoleKey);
            return settings != null
                ? settings.ChamsEnabled.Value
                : ShouldShowChams(target.Kind);
        }

        private Color GetRoleChamColor(
            EspRoleSettings settings,
            bool hidden)
        {
            return ParseVisualColor(
                hidden
                    ? settings.ChamHiddenColor.Value
                    : settings.ChamVisibleColor.Value,
                hidden
                    ? settings.DefaultHidden
                    : settings.DefaultVisible);
        }

        private sealed class EspRoleSettings
        {
            public string Key;
            public string Label;
            public string Group;
            public EspKind Kind;
            public ConfigEntry<bool> Enabled;
            public ConfigEntry<string> VisibleColor;
            public ConfigEntry<string> HiddenColor;
            public ConfigEntry<bool> ChamsEnabled;
            public ConfigEntry<string> ChamVisibleColor;
            public ConfigEntry<string> ChamHiddenColor;
            public Color DefaultVisible;
            public Color DefaultHidden;
            public ChamMaterialSet ChamMaterials;
            public string LastChamVisible;
            public string LastChamHidden;
            public float LastChamOpacity = -1f;
        }

        private sealed class EspRoleGroup
        {
            public string Name;
            public bool Expanded;
            public bool ChamsExpanded;
            public readonly List<EspRoleSettings> Roles =
                new List<EspRoleSettings>();
        }
    }
}
