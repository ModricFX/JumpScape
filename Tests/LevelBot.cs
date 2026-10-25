using System;
using System.Collections.Generic;
using System.Linq;
using JumpScape.Gameplay;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// An automated player: it plays a level with real inputs (walk, jump, throw, E) from start to finish.
partial class TestGame
{
    /// <summary>
    /// A stretch of a platform the player can walk along freely. Low platforms above another platform
    /// (like a step on the ground) block walking, so they split it into several stretches.
    /// MinX/MaxX are the player's left edge.
    /// </summary>
    record Stretch(int Platform, int MinX, int MaxX);

    /// <summary>Window: how many pixels wide the range of take-off points is that make this move work.</summary>
    record Edge(int To, int StartX, int Dir, int Delay, bool Jump, float LandX, int Window);

    sealed class AreaMap
    {
        public List<Stretch> Stretches = new List<Stretch>();
        public Dictionary<int, List<Edge>> Edges = new Dictionary<int, List<Edge>>();
    }

    sealed class BotReport
    {
        public bool Finished;
        public float Seconds;
        public int Jumps;
        public int Throws;
        public int HeartsLost;
        public int GemsFound;
        public int GemCount;
        public List<string> MissedGems = new List<string>();
        public int Replans;
        public List<int> Windows = new List<int>();
        public string Problem;
    }

    readonly Dictionary<Area, AreaMap> _maps = new Dictionary<Area, AreaMap>();

    /// <summary>The spikes of the area being mapped. Moves that touch them don't count.</summary>
    List<Rectangle> _spikes = new List<Rectangle>();

    AreaMap MapFor(Area area)
    {
        if (_maps.TryGetValue(area, out var map)) return map;
        map = new AreaMap();
        var platforms = area.Platforms;
        _spikes = area.Spikes.Select(spikes => spikes.Hitbox).ToList();
        for (int i = 0; i < platforms.Count; i++)
            map.Stretches.AddRange(StretchesOf(i, platforms, _spikes));
        for (int s = 0; s < map.Stretches.Count; s++)
            map.Edges[s] = FindEdges(map, s, platforms);
        _maps[area] = map;
        return map;
    }

    static IEnumerable<Stretch> StretchesOf(int index, List<Platform> platforms, List<Rectangle> spikes)
    {
        var b = platforms[index].Bounds;
        int min = Math.Max(0, b.Left - PW + 1), max = Math.Min(1920 - PW, b.Right - 1);
        // Where the player would bump into another platform, or stand in spikes, while standing here.
        var standing = new Rectangle(b.Left - PW, b.Top - PH, b.Width + 2 * PW, PH);
        var blocked = platforms.Where((pl, i) => i != index && pl.Bounds.Intersects(standing) && pl.Bounds.Top < b.Top).Select(pl => pl.Bounds)
            .Concat(spikes.Where(spike => spike.Intersects(standing)))
            .Select(r => (From: r.Left - PW + 1, To: r.Right - 1)).OrderBy(r => r.From).ToList();

        int start = min;
        foreach (var (from, to) in blocked)
        {
            if (from > start) yield return new Stretch(index, start, Math.Min(from - 1, max));
            start = Math.Max(start, to + 1);
        }
        if (start <= max) yield return new Stretch(index, start, max);
    }

    static int StretchAt(AreaMap map, int platform, float x)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int s = 0; s < map.Stretches.Count; s++)
        {
            var st = map.Stretches[s];
            if (st.Platform != platform) continue;
            float d = x < st.MinX ? st.MinX - x : x > st.MaxX ? x - st.MaxX : 0f;
            if (d < bestDistance) { bestDistance = d; best = s; }
        }
        return best;
    }

    static Rectangle StandingArea(Stretch stretch, List<Platform> platforms)
    {
        var b = platforms[stretch.Platform].Bounds;
        return new Rectangle(stretch.MinX, b.Top - PH, stretch.MaxX - stretch.MinX + PW, PH);
    }

    /// <summary>
    /// Simulates jumps from many starting points and keeps, for each stretch it can land on (and each direction),
    /// the move that works from the widest range of starting points, so small differences in the real run don't matter.
    /// </summary>
    List<Edge> FindEdges(AreaMap map, int from, List<Platform> platforms)
    {
        var stretch = map.Stretches[from];
        var successes = new Dictionary<(int To, int Dir, int Delay, bool Jump), List<int>>();
        for (int x = stretch.MinX; x <= stretch.MaxX; x += 6)
        foreach (int dir in new[] { -1, 0, 1 })
        foreach (int delay in new[] { 0, 6, 12, 18, 24, 32 })
        foreach (bool jump in new[] { true, false })
        {
            if (!jump && (dir == 0 || delay > 0)) continue;
            int to = SimulateMove(map, from, platforms, x, dir, delay, jump, out _);
            if (to < 0 || to == from) continue;
            var key = (to, dir, delay, jump);
            if (!successes.TryGetValue(key, out var list)) successes[key] = list = new List<int>();
            list.Add(x);
        }

        var edges = new List<Edge>();
        foreach (var group in successes.GroupBy(pair => (pair.Key.To, Math.Sign(pair.Key.Dir))))
        {
            var best = group.Select(pair => (pair.Key, Run: LongestRun(pair.Value))).OrderByDescending(c => c.Run.Count).ThenBy(c => c.Key.Jump ? 1 : 0).First();
            int startX = best.Run[best.Run.Count / 2];
            SimulateMove(map, from, platforms, startX, best.Key.Dir, best.Key.Delay, best.Key.Jump, out float landX);
            edges.Add(new Edge(group.Key.To, startX, best.Key.Dir, best.Key.Delay, best.Key.Jump, landX, best.Run.Count * 6));
        }
        Press();
        return edges;
    }

    static List<int> LongestRun(List<int> xs)
    {
        List<int> best = new List<int>(), current = new List<int>();
        foreach (int x in xs.OrderBy(x => x))
        {
            if (current.Count > 0 && x - current[^1] > 6) current = new List<int>();
            current.Add(x);
            if (current.Count > best.Count) best = current;
        }
        return best;
    }

    /// <summary>Plays one move with the real player physics; returns the stretch it ends on, or -1.</summary>
    int SimulateMove(AreaMap map, int from, List<Platform> platforms, int x, int dir, int delay, bool jump, out float landX)
    {
        landX = 0f;
        int fromPlatform = map.Stretches[from].Platform;
        var p = new Player(new Vector2(x, platforms[fromPlatform].Bounds.Top - PH));
        try
        {
            Press(); p.Update(Dt, NoKeys); p.ApplyGravity(); p.ResolveCollisions(platforms);
            if (!p.IsOnGround || PlatformUnder(p, platforms) != fromPlatform) return -1;
            for (int f = 0; f < 240; f++)
            {
                var keys = new List<Keys>();
                if (jump && (f == 0 || Rising(p))) keys.Add(Keys.Space);
                if (dir != 0 && f >= delay) keys.Add(dir < 0 ? Keys.Left : Keys.Right);
                Press(keys.ToArray());
                p.Update(Dt, NoKeys); p.ApplyGravity(); p.ResolveCollisions(platforms);
                if (_spikes.Any(spike => spike.Intersects(p.Bounds))) return -1;
                if (f > 1 && p.IsOnGround)
                {
                    int under = PlatformUnder(p, platforms);
                    if (under < 0) continue;
                    int stretch = StretchAt(map, under, p.Position.X);
                    if (stretch != from) { landX = p.Position.X; return stretch; }
                    if (jump) return -1;
                }
            }
            return -1;
        }
        finally
        {
            p.StopSounds();
            Microsoft.Xna.Framework.FrameworkDispatcher.Update();
        }
    }

    // ---------- playing ----------

    GameplayResult _botResult;
    int _botFrames;
    BotReport _report;

    void BotStep(GameplayScreen gs, Keys[] keys, Vector2? clickWorld = null)
    {
        float cameraTop = Get<float>(gs, "_cameraTop");
        if (clickWorld is Vector2 world)
            SetMouse(world - new Vector2(0, cameraTop), true);
        else
            SetMouse(new Vector2(-1000, -1000), false);
        Press(keys);
        var r = gs.Update(NextTime());
        if (r != GameplayResult.None) _botResult = r;
        _botFrames++;
    }

    bool BotTimeUp => _botFrames > 60 * 300;

    /// <summary>True if an orb thrown from one point to the other wouldn't hit a platform on the way.</summary>
    static bool ClearShot(Area area, Vector2 from, Vector2 to)
    {
        float length = Vector2.Distance(from, to);
        for (float d = 0; d < length; d += 8f)
        {
            var point = Vector2.Lerp(from, to, d / length).ToPoint();
            if (area.Platforms.Any(pl => pl.IsSolid && pl.Bounds.Contains(point))) return false;
        }
        return true;
    }

    /// <summary>The nearest frog or ghost the player could hit with an orb right now, if any.</summary>
    Vector2? EnemyToShoot(GameplayScreen gs)
    {
        var p = PlayerOf(gs);
        if (!p.CanThrow) return null;
        var area = AreaOf(gs);
        Vector2? best = null;
        float bestDistance = 650f;
        foreach (var t in area.Monsters.Select(m => m.Center).Concat(area.Ghosts.Select(g => g.Center)))
        {
            float d = Vector2.Distance(t, p.Center);
            if (d < bestDistance && ClearShot(area, p.Center, t)) { bestDistance = d; best = t; }
        }
        if (best != null) _report.Throws++;
        return best;
    }

    bool EnemyNear(GameplayScreen gs, float range)
    {
        var p = PlayerOf(gs);
        var area = AreaOf(gs);
        return area.Monsters.Any(m => Vector2.Distance(m.Center, p.Center) < range)
            || area.Ghosts.Any(g => Vector2.Distance(g.Center, p.Center) < range);
    }

    /// <summary>Walks until the player's left edge is near <paramref name="x"/>, throwing at enemies on the way.</summary>
    void WalkTo(GameplayScreen gs, float x)
    {
        var p = PlayerOf(gs);
        int start = PlatformUnder(p, AreaOf(gs).Platforms);
        float lastX = p.Position.X;
        int stuck = 0;
        bool jumping = false;
        for (int i = 0; i < 900 && !BotTimeUp && !p.IsDead && Math.Abs(p.Position.X - x) >= 3; i++)
        {
            var key = p.Position.X < x ? Keys.Right : Keys.Left;
            stuck = p.Position.X == lastX ? stuck + 1 : 0;
            lastX = p.Position.X;
            // Something unexpected in the way: jump over it, as a player would.
            jumping = (stuck >= 3 && p.IsOnGround) || (jumping && Rising(p));
            var keys = jumping ? new[] { key, Keys.Space } : new[] { key };
            BotStep(gs, keys, EnemyToShoot(gs));
            if (p.IsOnGround && PlatformUnder(p, AreaOf(gs).Platforms) is int under && under >= 0 && under != start) return;
        }
        BotStep(gs, Array.Empty<Keys>());
    }

    void PerformEdge(GameplayScreen gs, Edge edge, AreaMap map)
    {
        var p = PlayerOf(gs);
        // Deal with nearby enemies first, so they don't knock the player off mid-jump.
        for (int i = 0; i < 90 && EnemyNear(gs, 450f) && EnemyToShoot(gs) is Vector2 target; i++)
            BotStep(gs, Array.Empty<Keys>(), target);

        WalkTo(gs, edge.StartX);
        // Like a player would: wait for a crumbled (or shaking) platform to come back before jumping onto it,
        // unless the platform underfoot is about to go too.
        var platforms = AreaOf(gs).Platforms;
        var landing = platforms[map.Stretches[edge.To].Platform];
        bool Unsafe(Platform pl) => !pl.IsSolid || Get<bool>(pl, "_isCrumbling");
        for (int i = 0; i < 60 * 9 && Unsafe(landing) && !BotTimeUp; i++)
        {
            int under = PlatformUnder(p, platforms);
            if (under >= 0 && Unsafe(platforms[under])) break;
            BotStep(gs, Array.Empty<Keys>(), EnemyNear(gs, 450f) ? EnemyToShoot(gs) : null);
        }
        if (edge.Jump) _report.Jumps++;
        int start = CurrentStretch(gs, map);
        for (int f = 0; f < 240 && !BotTimeUp && !p.IsDead; f++)
        {
            var keys = new List<Keys>();
            if (edge.Jump && (f == 0 || Rising(p))) keys.Add(Keys.Space);
            if (edge.Dir != 0 && f >= edge.Delay) keys.Add(edge.Dir < 0 ? Keys.Left : Keys.Right);
            BotStep(gs, keys.ToArray());
            if (f > 1 && p.IsOnGround && (edge.Jump || CurrentStretch(gs, map) != start)) break;
        }
        BotStep(gs, Array.Empty<Keys>());
    }

    int CurrentStretch(GameplayScreen gs, AreaMap map)
    {
        var p = PlayerOf(gs);
        int under = PlatformUnder(p, AreaOf(gs).Platforms);
        return p.IsOnGround && under >= 0 ? StretchAt(map, under, p.Position.X) : -1;
    }

    /// <summary>Fewest moves to any goal stretch, finishing with the move that lands closest to the target.</summary>
    static List<Edge> FindPath(AreaMap map, int from, HashSet<int> goals, HashSet<(int, int)> banned, float targetX, int minWindow)
    {
        var previous = new Dictionary<int, (int From, Edge Edge)>();
        var queue = new Queue<int>();
        var seen = new HashSet<int> { from };
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            int at = queue.Dequeue();
            var finish = map.Edges[at].Where(e => e.Window >= minWindow && goals.Contains(e.To) && !banned.Contains((at, e.To)))
                .OrderBy(e => Math.Abs(e.LandX - targetX)).FirstOrDefault();
            if (finish != null)
            {
                var path = new List<Edge> { finish };
                int back = at;
                while (back != from) { var step = previous[back]; path.Insert(0, step.Edge); back = step.From; }
                return path;
            }
            foreach (var edge in map.Edges[at])
            {
                if (edge.Window < minWindow || banned.Contains((at, edge.To)) || !seen.Add(edge.To)) continue;
                previous[edge.To] = (at, edge);
                queue.Enqueue(edge.To);
            }
        }
        return null;
    }

    static void DumpMap(AreaMap map, int from, HashSet<int> goals, Area area)
    {
        var reach = new HashSet<int> { from };
        var queue = new Queue<int>(reach);
        while (queue.Count > 0)
            foreach (var e in map.Edges[queue.Dequeue()])
                if (reach.Add(e.To)) queue.Enqueue(e.To);
        for (int s = 0; s < map.Stretches.Count; s++)
        {
            var st = map.Stretches[s];
            var b = area.Platforms[st.Platform].Bounds;
            Console.WriteLine($"      {(reach.Contains(s) ? "R" : " ")}{(goals.Contains(s) ? "G" : " ")} s{s} platform {st.Platform} at ({b.X},{1017 - b.Y}) len {b.Width} x {st.MinX}..{st.MaxX} -> "
                + string.Join(", ", map.Edges[s].Select(e => $"s{e.To}")));
        }
    }

    bool _collectGems;
    readonly HashSet<Gem> _givenUpGems = new HashSet<Gem>();

    bool GemWanted(Gem gem) => _collectGems && !gem.IsCollected && !_givenUpGems.Contains(gem);

    /// <summary>
    /// The next thing to reach: a gem, a key, a portal on the way to gems, a key or the exit, or the exit door.
    /// Locked portals are only used when holding a key.
    /// </summary>
    (Rectangle Target, Door Door, Gem Gem, string Why) ChooseGoal(GameplayScreen gs)
    {
        var areas = Get<List<Area>>(gs, "_areas");
        var area = AreaOf(gs);
        var p = PlayerOf(gs);
        int keysHeld = Get<ItemType?[]>(p.Inventory, "_slots").Count(slot => slot == ItemType.Key);
        int lockedDoors = areas.SelectMany(a => a.Doors).Count(d => d.State == DoorState.Locked);

        var gem = area.Gems.Where(GemWanted).OrderBy(g => Vector2.Distance(g.Center, p.Center)).FirstOrDefault();
        if (gem != null)
            return (gem.Bounds, null, gem, "gem");

        var key = area.Keys.FirstOrDefault(k => !k.IsCollected);
        if (key != null && keysHeld < lockedDoors)
            return (key.Bounds, null, null, "key");

        bool PortalUsable(Door portal) => portal.State != DoorState.Locked || keysHeld > 0;
        Area Destination(Door portal) => areas.FirstOrDefault(a => a.Doors.Any(d => string.Equals(d.Name, portal.Target, StringComparison.OrdinalIgnoreCase)));

        // Breadth-first through the portals to the nearest area that has what's wanted.
        Door FirstPortalTowards(Func<Area, bool> wanted)
        {
            var first = new Dictionary<Area, Door>();
            var queue = new Queue<Area>();
            queue.Enqueue(area);
            var seen = new HashSet<Area> { area };
            while (queue.Count > 0)
            {
                var at = queue.Dequeue();
                if (at != area && wanted(at))
                    return first[at];
                foreach (var portal in at.Doors.Where(d => d.Target != null && PortalUsable(d)))
                {
                    var next = Destination(portal);
                    if (next == null || !seen.Add(next)) continue;
                    first[next] = at == area ? portal : first[at];
                    queue.Enqueue(next);
                }
            }
            return null;
        }

        if (FirstPortalTowards(a => a.Gems.Any(GemWanted)) is Door towardsGems)
            return (towardsGems.Bounds, towardsGems, null, "portal");

        bool keysElsewhere = keysHeld < lockedDoors && areas.Any(a => a != area && a.Keys.Any(k => !k.IsCollected));
        if (!keysElsewhere)
        {
            var exit = area.Doors.FirstOrDefault(d => d.Target == null && (d.State != DoorState.Locked || keysHeld > 0));
            if (exit != null)
                return (exit.Bounds, exit, null, "exit");
        }

        Door portalOnTheWay = FirstPortalTowards(a => keysElsewhere ? a.Keys.Any(k => !k.IsCollected) : a.Doors.Any(d => d.Target == null));
        return portalOnTheWay != null ? (portalOnTheWay.Bounds, portalOnTheWay, null, "portal") : (Rectangle.Empty, null, null, "nothing left to do");
    }

    /// <summary>A straight jump, holding jump until the top, then waits to land.</summary>
    void JumpUp(GameplayScreen gs)
    {
        var p = PlayerOf(gs);
        BotStep(gs, new[] { Keys.Space });
        for (int i = 0; i < 120 && !p.IsDead && (Rising(p) || !p.IsOnGround); i++)
            BotStep(gs, Rising(p) ? new[] { Keys.Space } : Array.Empty<Keys>());
    }

    void UseDoorLikeAPlayer(GameplayScreen gs, Door door)
    {
        var p = PlayerOf(gs);
        if (door.State == DoorState.Locked)
        {
            var slots = Get<ItemType?[]>(p.Inventory, "_slots");
            int slot = Array.IndexOf(slots, (ItemType?)ItemType.Key);
            if (slot < 0) return;
            BotStep(gs, new[] { Keys.D1 + slot }); BotStep(gs, Array.Empty<Keys>());
            BotStep(gs, new[] { Keys.E }); BotStep(gs, Array.Empty<Keys>());
        }
        if (door.State == DoorState.Closed) { BotStep(gs, new[] { Keys.E }); BotStep(gs, Array.Empty<Keys>()); }
        if (door.State == DoorState.Open) { BotStep(gs, new[] { Keys.E }); BotStep(gs, Array.Empty<Keys>()); }
        for (int i = 0; i < 40; i++) BotStep(gs, Array.Empty<Keys>());
    }

    /// <param name="collectGems">Also collects every gem, checking that they can all be reached.</param>
    BotReport PlayLevelWithBot(LevelData level, bool verbose = false, bool collectGems = true)
    {
        _report = new BotReport { GemCount = level.GemCount };
        _collectGems = collectGems;
        _givenUpGems.Clear();
        _botResult = GameplayResult.None;
        _botFrames = 0;
        var gs = new GameplayScreen(level, true);
        var banned = new HashSet<(int, int)>();
        var failures = new Dictionary<(int, int), int>();
        int touchFailures = 0;
        try
        {
            for (int i = 0; i < 60; i++) BotStep(gs, Array.Empty<Keys>());
            while (_botResult != GameplayResult.LevelCompleted)
            {
                var p = PlayerOf(gs);
                if (p.IsDead) { _report.Problem = "died"; break; }
                if (BotTimeUp) { _report.Problem = "ran out of time"; break; }
                if (p.ReachedExit) { BotStep(gs, Array.Empty<Keys>()); continue; }

                var area = AreaOf(gs);
                var map = MapFor(area);
                int here = CurrentStretch(gs, map);
                if (here < 0) { BotStep(gs, Array.Empty<Keys>()); continue; }

                var goal = ChooseGoal(gs);
                if (goal.Target == Rectangle.Empty) { _report.Problem = "no goal: " + goal.Why; break; }

                // Gems can float above a platform, so a jump straight up from below also reaches them.
                Rectangle Reach(int i)
                {
                    var standing = StandingArea(map.Stretches[i], area.Platforms);
                    if (goal.Gem != null) { standing.Y -= GemJumpReach; standing.Height += GemJumpReach; }
                    return standing;
                }
                var goalStretches = new HashSet<int>(Enumerable.Range(0, map.Stretches.Count).Where(i => Reach(i).Intersects(goal.Target)));
                if (goalStretches.Contains(here))
                {
                    var st = map.Stretches[here];
                    WalkTo(gs, MathHelper.Clamp(goal.Target.Center.X - PW / 2f, st.MinX, st.MaxX));
                    if (goal.Gem != null && !goal.Gem.IsCollected)
                        JumpUp(gs);
                    if (goal.Gem != null ? !goal.Gem.IsCollected : !p.Bounds.Intersects(goal.Target))
                    {
                        if (++touchFailures <= 8) continue;
                        if (goal.Gem == null) { _report.Problem = $"can't touch the {goal.Why} from stretch {here}"; break; }
                        GiveUpOn(goal.Gem, area, "can't be touched");
                        touchFailures = 0;
                        continue;
                    }
                    touchFailures = 0;
                    if (goal.Door != null) UseDoorLikeAPlayer(gs, goal.Door);
                    continue;
                }

                float targetX = goal.Target.Center.X - PW / 2f;
                var path = FindPath(map, here, goalStretches, banned, targetX, 24) ?? FindPath(map, here, goalStretches, banned, targetX, 0);
                if (path == null && goal.Gem != null)
                {
                    GiveUpOn(goal.Gem, area, "has no route to it");
                    continue;
                }
                if (path == null)
                {
                    _report.Problem = $"no route to the {goal.Why} from stretch {here} ({map.Stretches[here]}) in {area.Name}";
                    if (verbose) DumpMap(map, here, goalStretches, area);
                    break;
                }

                var edge = path[0];
                PerformEdge(gs, edge, map);
                int landed = CurrentStretch(gs, map);
                if (landed == edge.To && edge.Jump && map.Stretches[edge.To].Platform != 0) _report.Windows.Add(edge.Window);
                if (verbose && edge.Jump && edge.Window < 48 && map.Stretches[edge.To].Platform != 0) Console.WriteLine($"    tight: {area.Name} platform {map.Stretches[here].Platform} -> {map.Stretches[edge.To].Platform}, window {edge.Window} px");
                if (landed != edge.To)
                {
                    if (verbose) Console.WriteLine($"    miss: {area.Name} {here}->{edge.To} {edge}, landed on {landed}");
                    _report.Replans++;
                    var pair = (here, edge.To);
                    failures[pair] = failures.GetValueOrDefault(pair) + 1;
                    if (failures[pair] >= 3) banned.Add(pair);
                }
            }
            _report.Finished = _botResult == GameplayResult.LevelCompleted;
            _report.Seconds = Get<float>(gs, "_playTime");
            _report.HeartsLost = (6 - Health(PlayerOf(gs))) / 2;
            _report.GemsFound = gs.GemsFound.Count;
        }
        finally
        {
            gs.StopSounds();
            SetMouse(new Vector2(-1000, -1000), false);
            Press();
        }
        return _report;
    }

    /// <summary>How far above the platform's standing area a jump straight up still touches a gem.</summary>
    const int GemJumpReach = 330;

    void GiveUpOn(Gem gem, Area area, string why)
    {
        _givenUpGems.Add(gem);
        _report.MissedGems.Add($"gem at ({gem.Bounds.X}, groundY-{1017 - gem.Bounds.Y}) in {area.Name} {why}");
    }
}

partial class TestGame
{
    /// <summary>Saves a picture of every area of every level, tall enough to show the whole climb.</summary>
    void LevelOverviews()
    {
        foreach (int n in LevelData.FindLevelNumbers())
        {
            var level = LevelData.Load(n);
            var gs = new GameplayScreen(level, true);
            Frames(gs, 400);
            var areas = Get<List<Area>>(gs, "_areas");
            foreach (var area in areas)
            {
                int top = area.Platforms.Min(pl => pl.Bounds.Top) - 260;
                int height = Math.Max(1080, 1080 - Math.Min(0, top));
                Set(gs, "_area", area);
                Shot($"overview-{n}-{area.Name.Replace(' ', '-')}", sb =>
                {
                    Set(gs, "_cameraTop", 1080f - height);
                    gs.Draw(sb);
                }, area.Sky, 1920, height);
            }
            gs.StopSounds();
        }
    }
}
