using System;
using XRL.Rules;
using XRL.World;
using XRL.World.Capabilities;

#nullable disable
namespace XRL.World.Parts
{
    [Serializable]
    public class LaurusTerrainTileUnstable : LaurusStairsDown
    {
        public string CrumbledDisplayName;
        public string CrumbledRenderString = "?";
        public string CrumbledColorString = "&y";
        public string CrumbledTile;
        public int RenderLayer;
        public new bool Visible = true;
        public string LiquidNamePreposition;
        public bool AddStairHighlight;
        public bool Crumbled;
        public int Difficulty;

        public override void Initialize()
        {
            base.Initialize();

            if (!Crumbled)
                EnableCustomRender();
        }

        private void EnableCustomRender()
        {
            if (!ParentObject.Render.CustomRender && !ParentObject.HasIntProperty("CustomRenderSources"))
            {
                ParentObject.ModIntProperty("CustomRenderSources", 1);
            }
            ParentObject.Render.CustomRender = true;
            ParentObject.ModIntProperty("CustomRenderSources", 1);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (base.WantEvent(ID, cascade))
                return true;

            return ID == PooledEvent<CheckTileChangeEvent>.ID ||
                   (ID == GetAdjacentNavigationWeightEvent.ID && !Crumbled) ||
                   (ID == GetNavigationWeightEvent.ID && !Crumbled);
        }

        public override bool HandleEvent(GetNavigationWeightEvent E)
        {
            if (Crumbled)
                return base.HandleEvent(E);

            E.Weight = E.PriorWeight;
            return false;
        }

        public override bool HandleEvent(GetAdjacentNavigationWeightEvent E)
        {
            if (Crumbled)
                return base.HandleEvent(E);

            E.Weight = E.PriorWeight;
            return false;
        }

        public override bool HandleEvent(CheckTileChangeEvent E) => !Crumbled && base.HandleEvent(E);

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
            Registrar.Register("CustomRender");
            Registrar.Register("Searched");
        }

        public override bool FireEvent(Event E)
        {
            switch (E.ID)
            {
                case "CustomRender":
                    HandleCustomRenderEvent(E);
                    break;
                case "Searched":
                    HandleSearchedEvent(E);
                    break;
            }
            return base.FireEvent(E);
        }

        private void HandleCustomRenderEvent(Event E)
        {
            if (!Crumbled && E.GetParameter("RenderEvent") is RenderEvent parameter &&
                (parameter.Lit == LightLevel.Radar || parameter.Lit == LightLevel.LitRadar))
            {
                Crumble();
            }
        }

        private void HandleSearchedEvent(Event E)
        {
            if (!Crumbled)
            {
                GameObject searcher = E.GetGameObjectParameter("Searcher");
                int bonus = E.GetIntParameter("Bonus");
                if (Stat.RollResult(searcher.Stat("Intelligence")) + bonus >= Difficulty)
                {
                    Crumble();
                }
            }
        }

        public void Crumble()
        {
            if (Crumbled) return;

            Crumbled = true;
            UpdateRenderState();

            if (AddStairHighlight)
            {
                ParentObject.RequirePart<StairHighlight>();
            }

            UpdateLiquidPreposition();

            IComponent<GameObject>.AddPlayerMessage(
                $"{ParentObject.Does("are", WithIndefiniteArticle: true)} revealed {The.Player.DescribeDirectionToward(ParentObject)}!");

            InterruptAutoActIfNeeded();

            // **Trigger the fall when crumbled**
            if (IsPullDown && ParentObject.CurrentCell != null)
            {
                CheckPullDown(The.Player);
            }
        }

        private void UpdateRenderState()
        {
            ParentObject.ModIntProperty("CustomRenderSources", -1);

            if (ParentObject.GetIntProperty("CustomRenderSources") <= 0)
            {
                ParentObject.Render.CustomRender = false;
            }

            ParentObject.Render.DisplayName = CrumbledDisplayName;
            ParentObject.Render.RenderString = CrumbledRenderString;
            ParentObject.Render.ColorString = CrumbledColorString;
            ParentObject.Render.Tile = CrumbledTile;
            ParentObject.Render.RenderLayer = RenderLayer;
            ParentObject.Render.Visible = Visible;
        }

        private void UpdateLiquidPreposition()
        {
            if (!string.IsNullOrEmpty(LiquidNamePreposition))
            {
                LiquidVolume liquidVolume = ParentObject.LiquidVolume;
                if (liquidVolume != null)
                {
                    liquidVolume.NamePreposition = LiquidNamePreposition;
                }
            }
        }

        private void InterruptAutoActIfNeeded()
        {
            if (AutoAct.IsInterruptable() &&
                (!ParentObject.HasTag("Creature") || The.Player.IsRelevantHostile(ParentObject)))
            {
                AutoAct.Interrupt(null, null, ParentObject, true);
            }
        }

        // Override pull down conditions
        protected override bool IsValidForPullDown(GameObject obj)
        {
            return base.IsValidForPullDown(obj) && Crumbled && obj.CurrentCell.Equals(ParentObject.CurrentCell);
        }

    }
}
