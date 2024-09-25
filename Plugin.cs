using MCGalaxy;

public sealed class BanchoSpleef : Plugin {
    public override int build {
        get { return 0; }
    }

    public override string creator {
        get { return "siriusmart"; }
    }

    public override string name {
        get { return "BanchoSpleef"; }
    }

    public override void Load(bool startup) {
        Games.RegisterGame(typeof(SpleefDuelSolo));
    }

    public override void Unload(bool shutdown) {}
}
