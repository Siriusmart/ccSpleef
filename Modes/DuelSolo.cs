using MCGalaxy.Config;
using MCGalaxy.Network;
using MCGalaxy;
using BlockID = System.UInt16;

public sealed class SpleefDuelSolo : Game {
    public override string ModeName() { return StaticModeName(); }
    public override string ShortName() { return StaticShortName(); }

    public SpleefDuelSoloMapConfig MapConifg = new SpleefDuelSoloMapConfig();

    public new static string StaticModeName() { return "spleef-duel-solo";
    }

    public new static string StaticShortName() { return "sd1";
    }

    public override int BroadcastCountdown() { return -1; }
    public override int GameStartCountdown() { return players.Count == 2 ? 5 : -1; }

    public override int MaxPlayers {
        get { return 2; }
    }

    public override bool CanSpectate() { return !HasEnded; }

    public override void OnLoadEvent() { MapConifg.Load(MapConfigPath); }

    public override void OnPlayerMoveEvent(Player p, Position next, byte yaw,
                                           byte pitch, ref bool cancel) {
        if (p.level.GetBlock((ushort)(next.FeetBlockCoords.X),
                             (ushort)(next.FeetBlockCoords.Y),
                             (ushort)(next.FeetBlockCoords.Z)) == 9) {
            if (HasStarted) {
                if (HasEnded)
                    return;
                HasEnded = true;
                Player loser = p;
                Player winner = ((players[0] == p) ? players[1] : players[0]);

                loser.SendCpeMessage(CpeMessageType.BigAnnouncement, "&cYou Died");
                winner.SendCpeMessage(CpeMessageType.BigAnnouncement, "&aYou Win");

                MessageAll(Formatter.GameBarsWrap(
                    $"&eGame Ended\n{winner.ColoredName} &e(&awinner&e) vs {loser.ColoredName} &e(&closer&e)"));
                DelayedUnload(5);

            } else {
                p.SendPosition(map.SpawnPos, new Orientation());
            }
        }
    }

    static Random rng = new Random();
    public override void OnStartEvent() {
        if (rng.NextSingle() < 0.5) {
            players.Reverse();
        }

        foreach (Player p in players) {
            bool extBlocks = p.Session.hasExtBlocks;
            BlockID[] breakable = MapConifg.breakable();
            int count = breakable.Count();
            int size = extBlocks ? 5 : 4;
            byte[] bulk = new byte[count * size];

            for (int i = 0; i < count; i++) {
                bool place = false;
                bool delete = true;
                if (breakable[i] == Block.Air)
                    place &= delete;
                Packet.WriteBlockPermission(breakable[i], place, delete, extBlocks,
                                            bulk, i * size);
            }
            p.Send(bulk);
        }

        players[0].Send(Packet.VelocityControl(0, 0, 0, 1, 1, 1));
        players[0].SendPosition(
            Position.FromFeet((int)Math.Ceiling(MapConifg.Spawn1x * 32),
                              (int)Math.Ceiling(MapConifg.Spawn1y * 32),
                              (int)Math.Ceiling(MapConifg.Spawn1z * 32)),
            new Orientation(MapConifg.Spawn1Yaw, MapConifg.Spawn1Pitch));

        players[1].Send(Packet.VelocityControl(0, 0, 0, 1, 1, 1));
        players[1].SendPosition(
            Position.FromFeet((int)Math.Ceiling(MapConifg.Spawn2x * 32),
                              (int)Math.Ceiling(MapConifg.Spawn2y * 32),
                              (int)Math.Ceiling(MapConifg.Spawn2z * 32)),
            new Orientation(MapConifg.Spawn2Yaw, MapConifg.Spawn2Pitch));
    }

    public override void OnPlayerJoinEvent(Player p) {
        bool extBlocks = p.Session.hasExtBlocks;
        int count = p.Session.MaxRawBlock + 1;
        int size = extBlocks ? 5 : 4;
        byte[] bulk = new byte[count * size];

        for (int i = 0; i < count; i++) {
            BlockID block = Block.FromRaw((BlockID)i);
            bool place = false;
            bool delete = false;
            if (block == Block.Air)
                place &= delete;
            Packet.WriteBlockPermission((BlockID)i, place, delete, extBlocks, bulk,
                                        i * size);
        }
        p.Send(bulk);

        MessageAll(
            $"{p.ColoredName} &ejoined the game ({players.Count}/{MaxPlayers})");
    }

    public override void OnPlayerMidgameLeaveEvent(Player p) {
        if (HasEnded)
            return;

        HasEnded = true;
        Player loser = p;
        Player winner = ((players[0] == p) ? players[1] : players[0]);

        winner.SendCpeMessage(CpeMessageType.BigAnnouncement, "&aYou Win");

        MessageAll(Formatter.GameBarsWrap(
            $"&eGame Ended\n{winner.ColoredName} &e(&awinner&e) vs {loser.ColoredName} &e(&closer&e)"));
        DelayedUnload(5);
    }
}

public sealed class SpleefDuelSoloMapConfig {
    private static ConfigElement[]? cfg;

    [ConfigFloat("spawn-1-x", "Spawn", 0, 0)]
    public float Spawn1x = 0;
    [ConfigFloat("spawn-1-y", "Spawn", 0, 0)]
    public float Spawn1y = 0;
    [ConfigFloat("spawn-1-z", "Spawn", 0, 0)]
    public float Spawn1z = 0;
    [ConfigByte("spawn-1-yaw", "Spawn")]
    public byte Spawn1Yaw = 0;
    [ConfigByte("spawn-1-pitch", "Spawn")]
    public byte Spawn1Pitch = 0;

    [ConfigFloat("spawn-2-x", "Spawn", 0, 0)]
    public float Spawn2x = 0;
    [ConfigFloat("spawn-2-y", "Spawn", 0, 0)]
    public float Spawn2y = 0;
    [ConfigFloat("spawn-2-z", "Spawn", 0, 0)]
    public float Spawn2z = 0;
    [ConfigByte("spawn-2-yaw", "Spawn")]
    public byte Spawn2Yaw = 0;
    [ConfigByte("spawn-2-pitch", "Spawn")]
    public byte Spawn2Pitch = 0;

    [ConfigString("breakable", "Gameplay", "")]
    public string Breakable = "";

    public void Load(string path) {
        if (!File.Exists(path))
            Save(path);

        if (cfg == null)
            cfg = ConfigElement.GetAll(typeof(SpleefDuelSoloMapConfig));

        ConfigElement.ParseFile(cfg, path, this);
    }

    public void Save(string path) {
        if (cfg == null)
            cfg = ConfigElement.GetAll(typeof(SpleefDuelSoloMapConfig));

        using (StreamWriter w = new StreamWriter(path)) {
            ConfigElement.Serialise(cfg, w, this);
        }
    }

    public BlockID[] breakable() {
        List<BlockID> ids = new List<BlockID>();
        foreach (string entry in Breakable.Split(',')) {
            try {
                ids.Add((BlockID)Convert.ToUInt16(entry.Trim()));
            } catch {
            }
        }

        return ids.ToArray();
    }
}
