using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using Color = System.Drawing.Color;
using Version = System.Version;


namespace SexyAkali
{
    static class Program
    {
        public const string ChampionName = "Akali";

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        //Menu
        public static Menu Menu, ComboMenu, DrawMenu, MiscMenu, LineClear;

        //spells
        public static Spell.Targeted Q;
        public static Spell.Skillshot W;
        public static Spell.Active E;
        public static Spell.Targeted R;
        private static Item cutlass;
        private static Item botrk;
        private static Item hextech;
        private static Obj_AI_Base target;
        private static bool IsRUse => Player.HasBuff("AkaliShadowDance");
        // Main
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
            Bootstrap.Init(null);
        }

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Player.ChampionName != "Akali") return;
            else
            {
                Chat.Print("This is Addon for Akali Change Champion ");
            }
            Q = new Spell.Targeted(SpellSlot.Q, 600);
            W = new Spell.Skillshot(SpellSlot.W, 1, 0, 0);
            E = new Spell.Active(SpellSlot.E);
            R = new Spell.Targeted(SpellSlot.R, 800);

            Menu = MainMenu.AddMenu("Sexy Akali", "Sexy Akali");
            Menu.AddGroupLabel("Sexy Akali currently version 1.0 ");
            Menu.AddSeparator();
            Menu.AddLabel("Made By Zyzz ");

            ComboMenu = Menu.AddSubMenu("Combo", "Combo");
            ComboMenu.AddGroupLabel("Settings Combo");
            ComboMenu.AddLabel("Combo");
            ComboMenu.Add("QCombo", new CheckBox("Use Q on Combo"));
            ComboMenu.Add("ECombo", new CheckBox("Use E on Combo"));
            ComboMenu.Add("RCombo", new CheckBox("Use R on Combo"));
            ComboMenu.Add("q.undertower", new CheckBox("Dont Use R on if target under turret"));

            DrawMenu = Menu.AddSubMenu("Drawing settings", "drawinsSection");
            DrawMenu.AddGroupLabel("Settings Drawings");
            DrawMenu.AddSeparator();
            DrawMenu.Add("draw.Q", new CheckBox("Draw Q range"));
            DrawMenu.Add("draw.W", new CheckBox("Draw W range"));
            DrawMenu.Add("draw.E", new CheckBox("Draw E range"));
            DrawMenu.Add("draw.R", new CheckBox("Draw R range"));
            DrawMenu.Add("draw.off", new CheckBox("Draw off all"));

            LineClear = Menu.AddSubMenu("Settings LeaneClear", "Settings LeaneClear");
            LineClear.AddGroupLabel("Settings LeaneClear");
            LineClear.AddSeparator();
            LineClear.Add("laneQ", new CheckBox("Use Q on LaneClear"));
            LineClear.Add("laneE", new CheckBox("Use E on LaneClear"));
            LineClear.Add("Laneclear Energy", new Slider(" % Energy", 10, 50, 0));

            MiscMenu = Menu.AddSubMenu("Misc");
            MiscMenu.AddGroupLabel("Settings Misc");
            MiscMenu.AddLabel("Anti Gap Closer");
            MiscMenu.Add("enable.antigap", new CheckBox("Enable Anti GapCloser"));
            MiscMenu.Add("panickey", new KeyBind("Panic Mode", false, KeyBind.BindTypes.HoldActive, 'A'));
            MiscMenu.AddLabel("KillSteal");
            MiscMenu.Add("Rkill", new CheckBox("Use R KillSteal"));
            MiscMenu.AddLabel("Ignite And Item");
            MiscMenu.Add("ignite", new CheckBox("Use Ignite?"));
            MiscMenu.Add("cutlass", new CheckBox("Use Cutlass?"));
            MiscMenu.Add("botrk", new CheckBox("Use Botrk?"));
            MiscMenu.Add("hextech", new CheckBox("Use Hextech?"));

            if (MiscMenu["KillSteal"].Cast<CheckBox>().CurrentValue && EntityManager.Heroes.Enemies.Any(it => R.IsInRange(it))) KillSteal();

            cutlass = new Item(3144, 450);
            botrk = new Item(3153, 450);
            hextech = new Item(3146, 700);


            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += OnDraw;
            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;

    
        }

        private static void Game_OnTick(EventArgs args)
        {
            if (MiscMenu["panickkey"].Cast<KeyBind>().CurrentValue)
            {
                W.Cast(Player);
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                LaneClear();
            }
            KillSteal();
        }
        private static bool UnderTheirTower(Obj_AI_Base target)
        {
            var tower =
                ObjectManager
                    .Get<Obj_AI_Turret>()
                    .FirstOrDefault(turret => turret != null && turret.Distance(target) <= 775 && turret.IsValid && turret.Health > 0 && !turret.IsAlly);

            return tower != null;
        }
        public static float SpellDamage(Obj_AI_Base target, SpellSlot slot)
        {
            switch (slot)
            {
                case SpellSlot.R:
                    return Damage.CalculateDamageOnUnit(Player, target, DamageType.Magical, new float[] { 100, 175, 250 }[R.Level - 1] + Player.TotalMagicalDamage);
                case SpellSlot.E:
                    return Damage.CalculateDamageOnUnit(Player, target, DamageType.Magical, new float[] { 30, 55, 80, 105, 130 }[E.Level - 1] + Player.TotalMagicalDamage);

                default:
                    return 0;
            }
        }
        public static float GetDamage(SpellSlot spell, Obj_AI_Base target)
        {
            float ap = Player.FlatMagicDamageMod + Player.BaseAbilityDamage;
            float ad = Player.FlatMagicDamageMod + Player.BaseAttackDamage;
            if (spell == SpellSlot.Q)
            {
                if (!Q.IsReady())
                    return 0;
                return Player.CalculateDamageOnUnit(target, DamageType.Magical, 25f + 35f * (Q.Level - 1) + 100 / 100 * ad);
            }
            return 0;
        }


        private static void OnDraw(EventArgs args)
        {
            var Target = TargetSelector.GetTarget(R.Range, DamageType.Physical);
            if (DrawMenu["draw.off"].Cast<CheckBox>().CurrentValue)
            {
                return;;
            }

            if (DrawMenu["draw.Q"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Gold, BorderWidth = 1, Radius = Q.Range }.Draw(Player.Position);
            }
            if (DrawMenu["draw.W"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Gold, BorderWidth = 1, Radius = W.Range }.Draw(Player.Position);
            }
            if (DrawMenu["draw.E"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Gold, BorderWidth = 1, Radius = E.Range }.Draw(Player.Position);
            }
            if (DrawMenu["draw.R"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Gold, BorderWidth = 1, Radius = R.Range }.Draw(Player.Position);
            }
        }

        private static void Combo()
        {
            var x = TargetSelector.GetTarget(R.Range, DamageType.Magical);
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var useE = (ComboMenu["ECombo"].Cast<CheckBox>().CurrentValue);
            var useQ = (ComboMenu["QCombo"].Cast<CheckBox>().CurrentValue);
            var useW = (ComboMenu["WCombo"].Cast<CheckBox>().CurrentValue);
            var useR = (ComboMenu["RCombo"].Cast<CheckBox>().CurrentValue);


            //Items use
            if (x != null && Player.Distance(x) <= botrk.Range)
            {
                botrk.Cast(x);
            }
                    
            if (x != null && Player.Distance(x) <= cutlass.Range)
            {
                cutlass.Cast(x);
            }

            if (x != null && Player.Distance(x) <= hextech.Range)
            {
                hextech.Cast(x);
            }

            //Now Combo *_*
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
            {
                if (Q.IsReady() && useQ && !target.IsDead && !target.IsZombie && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }
                if (useE && E.IsReady() && !target.IsDead && !target.IsZombie && target.IsValidTarget(325))
                {
                    E.Cast();
                }
                if (useR && R.IsReady() && !target.IsDead && !target.IsZombie && target.IsValidTarget(R.Range))
                {
                    if (UnderTheirTower(target))
                        if (ComboMenu["q.undertower"].Cast<CheckBox>().CurrentValue) return;
                    R.Cast(target);
                }
            }
                

        }

        private static void KillSteal()
        {
            if (R.IsReady())
            {
                var bye =
                    EntityManager.Heroes.Enemies.FirstOrDefault(
                        enemy => enemy.IsValidTarget(R.Range) && SpellDamage(enemy, SpellSlot.R) >= enemy.Health);
                if (bye != null)
                {
                    R.Cast(bye); return;
                }
            }
        }
        private static void LaneClear()
        {
            var useQ = LineClear["Qlc"].Cast<CheckBox>().CurrentValue;
            var useE = LineClear["Elc"].Cast<CheckBox>().CurrentValue;
            var minions = ObjectManager.Get<Obj_AI_Base>().OrderBy(m => m.Health).Where(m => m.IsMinion && m.IsEnemy && !m.IsDead);
            foreach (var minion in minions)
                if (useQ && Q.IsReady() && minion.IsValidTarget(Q.Range) && minion.Health <= GetDamage(SpellSlot.Q, minion))
                {
                    Q.Cast(minion);
                }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            var antigap = (MiscMenu["enable.antigap"].Cast<CheckBox>().CurrentValue);
            if (antigap && W.IsReady() && sender.IsValidTarget(W.Range) && sender.IsFacing(Player))
            {
                W.Cast(sender);
            }

        }
    }
}
