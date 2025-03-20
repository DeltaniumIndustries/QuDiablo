// Decompiled with JetBrains decompiler
// Type: XRL.Liquids.LiquidAcid
// Assembly: Assembly-CSharp, Version=2.0.209.44, Culture=neutral, PublicKeyToken=null
// MVID: BA3BEED9-964F-45DE-99ED-0720870557CB
// Assembly location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
// XML documentation location: C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.xml

using System;
using System.Collections.Generic;
using System.Text;
using XRL.Collections;
using XRL.Core;
using XRL.Rules;
using XRL.World;
using XRL.World.Effects;
using XRL.World.Parts;

#nullable disable
namespace XRL.Liquids
{
    [IsLiquid]
    [Serializable]
    public class LiquidAcid : LaurusFluidBase
    {

        protected static readonly string FLUID_NAME = "LaurusFluidStrongAcid";
        public LiquidAcid() : base(FLUID_NAME)
        {
        }

        public override bool Drank(
          LiquidVolume Liquid,
          int Volume,
          GameObject Target,
          StringBuilder Message,
          ref bool ExitInterface)
        {
            Message.Compound("{{G|IT BURNS!}}");
            string Dice = (Liquid.Proportion("acid") / 100 + 1).ToString() + "d10";
            Target.TakeDamage(Dice.Roll(), "from {{G|drinking acid}}!", "Acid", Owner: Target, Attacker: Liquid.ParentObject);
            ExitInterface = true;
            return true;
        }

        public override void FillingContainer(GameObject Container, LiquidVolume Liquid)
        {
            if (!SafeContainer(Container))
            {
                Container.ApplyEffect((Effect)new ContainedAcidEating());
            }
            base.FillingContainer(Container, Liquid);
        }

        public void ApplyAcid(LiquidVolume Liquid, GameObject GO, GameObject By, bool FromCell = false)
        {
            int exposureMillidrams = Liquid.GetLiquidExposureMillidrams(GO, "acid");
            int num1 = exposureMillidrams / 20000 + Stat.Random(1, exposureMillidrams) / 10000 + (Stat.Random(0, 10000) < exposureMillidrams ? 1 : 0) + (Stat.Random(0, 100000) < exposureMillidrams ? 4 : 0);
            GameObject gameObject = GO;
            int Amount = num1;
            bool flag = FromCell;
            GameObject Attacker = By ?? Liquid.ParentObject;
            int num2 = flag ? 1 : 0;
            gameObject.TakeDamage(Amount, "from {{G|acid}}!", "Acid", Attacker: Attacker, Environmental: num2 != 0, SilentIfNoDamage: true);
        }

        public override void SmearOn(
          LiquidVolume Liquid,
          GameObject Target,
          GameObject By,
          bool FromCell)
        {
            base.SmearOn(Liquid, Target, By, FromCell);
            this.ApplyAcid(Liquid, Target, By, FromCell);
        }

        public override void SmearOnTick(
          LiquidVolume Liquid,
          GameObject Target,
          GameObject By,
          bool FromCell)
        {
            base.SmearOnTick(Liquid, Target, By, FromCell);
            this.ApplyAcid(Liquid, Target, By, FromCell);
        }

        public override int GetNavigationWeight(
          LiquidVolume Liquid,
          GameObject GO,
          bool Smart,
          bool Slimewalking,
          bool FilthAffinity,
          ref bool Uncacheable)
        {
            if (Smart && GO != null)
            {
                Uncacheable = true;
                int num1 = GO.Stat("AcidResistance");
                if (num1 > 0)
                {
                    float val1 = 0.0f;
                    if (Liquid.IsSwimmingDepth())
                    {
                        using (ScopeDisposedList<GameObject> fromPool = ScopeDisposedList<GameObject>.GetFromPool())
                        {
                            GO.GetContents((IList<GameObject>)fromPool);
                            foreach (GameObject gameObject in (PooledContainer<GameObject>)fromPool)
                            {
                                if (!gameObject.IsNatural() && !gameObject.HasPart<NoDamage>() && !gameObject.HasPart<NoDamageExcept>())
                                {
                                    int num2 = gameObject.Stat("AcidResistance");
                                    if (num2 < 100)
                                        val1 += (float)((gameObject.Equipped != null ? 1.0 : 0.5) * (double)(100 - num2) / 100.0);
                                }
                            }
                        }
                    }
                    else
                    {
                        List<GameObject> Return = Event.NewGameObjectList();
                        GO.Body?.GetEquippedObjectsExceptNatural(Return);
                        foreach (GameObject gameObject in Return)
                        {
                            if (!gameObject.HasPart<NoDamage>() && !gameObject.HasPart<NoDamageExcept>())
                            {
                                int num3 = gameObject.Stat("AcidResistance");
                                if (num3 < 100)
                                    val1 += (float)(1.0 * (double)(100 - num3) / 100.0);
                            }
                        }
                    }
                    int val2 = Math.Min((int)val1, 95);
                    return num1 >= 100 ? val2 : Math.Min(Math.Max((65 + val2) * (100 - num1) / 100, val2), 99);
                }
            }
            return 30;
        }

        public override float GetValuePerDram() => 1.5f;

        public override string GetFluidID()
        {
            return FLUID_NAME;
        }

        public override string GetColourStringPrimary()
        {
            return "O";
        }

        public override string GetColourStringSecondary()
        {
            return "G";
        }

        public override string GetDetailColour()
        {
            return "G";
        }

        public override string GetTileColour()
        {
            return "O";
        }

        protected override int GetFluidEvaporativity()
        {
            return 0;
        }

        protected override int GetFluidThermalConductivity()
        {
            return 0;
        }

        protected override int GetFluidCombuestibility()
        {
            return 0;
        }

        protected override int GetFluidVapourTemp()
        {
            return 1200;
        }

        protected override int GetFluidFlameTemp()
        {
            return 700;
        }

        protected override string GetFluidName()
        {
            return "Industrious Waste Acid";
        }

        protected override string GetShaderString()
        {
            return "o";
        }

        protected override string GetFluidAdjective()
        {
            return "acidic";
        }

        protected override string GetFluidSmearedName()
        {
            return "acid-covered";
        }

        protected override string GetFluidPreparedCookingIngredient()
        {
            return "acidMajor";
        }


        protected override string GetFluidVapourObject()
        {
            return "";
        }

        protected override bool IsFluidDangerousToDrink()
        {
            return true;
        }

        protected override bool IsFluidDangerousToContact()
        {
            return true;
        }

        protected override bool DoesFluidInterruptAutoWalk()
        {
            return true;
        }

        public override bool SafeContainer(GameObject GO)
        {
            return !GO.IsOrganic;
        }
    }
}
