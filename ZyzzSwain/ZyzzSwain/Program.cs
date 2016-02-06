using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;

namespace ZyzzSwain
{
    internal class Program
    {
        public static Spell.Targeted Q, E;
        public static Spell.Skillshot W;
        public static Spell.Active R;
        private const string Champion = "Swain";
        private static Menu Config, ComboMenu, HarrasMenu, LaneClearMenu, LastHitMenu;
        private static Item Zhonya;
        private static bool RavenForm;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Game_Update;
        }

        private static void Game_Update(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != Champion) return;
            Q = new Spell.Targeted(SpellSlot.Q, 625);
            W = new Spell.Skillshot(SpellSlot.W, 820, SkillShotType.Circular, 500, 1250, 275);
            E = new Spell.Targeted(SpellSlot.E, 625);
            R = new Spell.Active(SpellSlot.R);

            Zhonya = new Item(3157);

            Config = MainMenu.AddMenu("ZyzzSwain", "ZyzzSwain");


            //Combo
            Config.AddGroupLabel("Combo Menu");

            Config.Add("C_UseQ", new CheckBox("Use Q"));
            Config.Add("C_UseW", new CheckBox("Use W"));
            Config.Add("C_UseE", new CheckBox("Use E"));
            Config.Add("C_UseR", new CheckBox("Use R"));
            Config.Add("C_MockingSwain", new CheckBox("Use Zhonya while Ult"));
            Config.Add("C_MockingSwainSlider", new Slider("Zhonya ult at Health (%)", 30, 0, 100));

            //Harras

            Config.AddGroupLabel("Harras Menu");

            Config.Add("H_UseQ", new CheckBox("Use Q"));
            Config.Add("H_UseW", new CheckBox("Use W"));
            Config.Add("H_UseE", new CheckBox("Use E"));
            Config.Add("H_AutoE", new CheckBox("Auto-E enemies"));
            Config.Add("H_ESlinder", new Slider("(Broken)Stop Auto E at Mana (%)", 30, 0, 100));

            //Lane Clear

            Config.AddGroupLabel("Lane Clear Menu (Broken)");

            Config.Add("LC_UseW", new CheckBox("Use W"));
            Config.Add("LC_UseR", new CheckBox("Use R"));

            //Last Hit
            Config.AddGroupLabel("Last Hit Menu");

            Config.Add("LH_UseQ", new CheckBox("Use Q"));
            Config.Add("LH_UseE", new CheckBox("Use E"));

            Chat.Print("ZyzzSwian loaded, Have fun!");
            Game.OnUpdate += Game_OnUpdate;
            GameObject.OnCreate += OnCreateObject;
            GameObject.OnDelete += OnDeleteObject;
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Combo)
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.Harass)
            {
                Harras();
            }
            if (Orbwalker.ActiveModesFlags == Orbwalker.ActiveModes.LastHit)
            {
                LastHit();
            }
            if (Config["H_AutoE"].Cast<CheckBox>().CurrentValue)
            {
                AutoE();
            }
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (!(sender.Name.Contains("swain_demonForm")))
                return;
            RavenForm = true;
        }
        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (!(sender.Name.Contains("swain_demonForm")))
                return;
            RavenForm = false;
        }

        private static void AutoE()
        {
            //Local
            var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var ManaLimit = Player.MaxMana / 100 * Config["H_ESlider"].Cast<Slider>().CurrentValue;

            if (Player.Mana <= ManaLimit) return;
            if (E.IsReady()) 
            {
                E.Cast(target);
            }
        }

        private static void MockingSwain()
        {
            //Local
            var HealthLimit = Player.MaxHealth / 100 * Config["C_MockingSwainSlider"].Cast<Slider>().CurrentValue;


            if (!RavenForm || !(Player.Health <= HealthLimit)) return;
            if (Zhonya.IsReady())
            {
                Zhonya.Cast();
            }
        }

        private static bool SafeWCast(AIHeroClient target)
        {
            if (target == null)
                return false;

            if (W.GetPrediction(target).HitChance == HitChance.Immobile)
                return true;
            if (target.HasBuffOfType(BuffType.Slow) && W.GetPrediction(target).HitChance >= HitChance.High)
                return true;
            return W.GetPrediction(target).HitChance == HitChance.High;
        }


        private static void Combo()
        {
            //Local
            var target = TargetSelector.GetTarget(800, DamageType.Magical);
            var useQ = Config["C_UseQ"].Cast<CheckBox>().CurrentValue;
            var useW = Config["C_UseW"].Cast<CheckBox>().CurrentValue;
            var useE = Config["C_UseE"].Cast<CheckBox>().CurrentValue;
            var useR = Config["C_UseR"].Cast<CheckBox>().CurrentValue;


            if (Config["C_MockingSwain"].Cast<CheckBox>().CurrentValue)
            {
                MockingSwain();
            }


            if (target == null) return;

            //E
            if (E.IsReady() && useE)
            {
                E.Cast(target);
            }
            //Q
            if (Q.IsReady() && useQ)
            {
                Q.Cast(target);
            }
            //W
            if (target.IsValidTarget(W.Range) && W.IsReady() && SafeWCast(target) && useW)
            {
                var pred = W.GetPrediction(target);
                W.Cast(pred.CastPosition);
            }
            //R
            if (R.IsReady() && target.IsValidTarget(R.Range) && !RavenForm && useR)
            {
                R.Cast();
            }


        }
        private static void Harras()
        {
            //Local
            var target = TargetSelector.GetTarget(800, DamageType.Magical);
            var useQ = Config["H_UseQ"].Cast<CheckBox>().CurrentValue;
            var useW = Config["H_UseW"].Cast<CheckBox>().CurrentValue;
            var useE = Config["H_UseE"].Cast<CheckBox>().CurrentValue;
            if (target == null) return;

            //E
            if (E.IsReady() && useE)
            {
                E.Cast(target);
            }
            //Q
            if (Q.IsReady() && useQ)
            {
                Q.Cast(target);
            }
            //W
            if (target.IsValidTarget(W.Range) && W.IsReady() && SafeWCast(target) && useW)
            {
                var pred = W.GetPrediction(target);
                W.Cast(pred.CastPosition);
            }

        }
        private static void LastHit()
        {
            //Local
            var useQ = Config["LH_UseQ"].Cast<CheckBox>().CurrentValue;
            var useE = Config["LH_UseE"].Cast<CheckBox>().CurrentValue;
            var Minions = EntityManager.MinionsAndMonsters.GetLaneMinions(
                EntityManager.UnitTeam.Enemy, Player.Position, Q.Range);
            foreach (var minion in Minions)
            {
                if (useQ)
                {
                    if (minion.Health < Player.GetSpellDamage(minion, SpellSlot.Q) && Q.IsReady())
                    {
                        Q.Cast(minion);
                        return;
                    }
                }
                if (useE)
                {
                    if (minion.Health < Player.GetSpellDamage(minion, SpellSlot.E) && E.IsReady())
                    {
                        E.Cast(minion);
                        return;
                    }
                }
                
            }
        }



    }

}
