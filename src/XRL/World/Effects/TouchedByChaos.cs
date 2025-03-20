using System;
using Qud.API;
using XRL.Rules;

namespace XRL.World.Effects
{
    [Serializable]
    public class TouchedByChaos : Mutating
    {
        public int MutationCount { get; private set; }

        public TouchedByChaos(int mutationCount, string population = "MutatingResults")
            : base(0, population) // Setting Duration to 0 to make it instant
        {
            MutationCount = mutationCount;
            DisplayName = $"{{{{{ColourUtils.GetRandomShaderType()}|corrupted by chaos}}}}";
        }

        public override string GetDetails()
        {
            return "Bones and flesh are torn and moulded anew.";
        }

        public override bool Apply(GameObject Object)
        {
            // Prevent players from receiving this effect
            if (Object.IsPlayer())
            {
                LL.Info($"[InstantMutating] Skipping effect: {Object.DisplayName} is a player.", LogCategory.Debug);
                return false;
            }

            // Check if the object is eligible
            if (!Object.IsMutant() && !Object.IsTrueKin())
            {
                LL.Info($"[InstantMutating] Skipping effect: {Object.DisplayName} is not a mutant or true kin.", LogCategory.Debug);
                return false;
            }
            if (Object.HasEffect<Mutating>())
            {
                LL.Info($"[InstantMutating] Skipping effect: {Object.DisplayName} already has a mutation effect.", LogCategory.Debug);
                return false;
            }
            if (!Object.FireEvent("ApplyMutating"))
            {
                LL.Info($"[InstantMutating] Skipping effect: {Object.DisplayName} rejected ApplyMutating event.", LogCategory.Debug);
                return false;
            }

            LL.Info($"[InstantMutating] Applying {MutationCount} mutations to {Object.DisplayName}", LogCategory.Debug);

            Object.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_neutral-weirdVitality");

            for (int i = 0; i < MutationCount; i++)
            {
                ApplySingleMutation(Object, i);
            }

            return base.Apply(Object);
        }

        private void ApplySingleMutation(GameObject Object, int additionalMutations)
        {
            bool isDefect = Stat.Random(1, 100) <= 20;
            string blueprint = isDefect ? "Defect" : "Mutation";
            int getBoostChance = Math.Max(10, 100 - (additionalMutations * 15));
            boostRandomStats(Object, getBoostChance);
            boostRandomResists(Object, getBoostChance);

            switch (blueprint)
            {
                case "Mutation":
                    MutationEntry newMutation = MutationsAPI.FindRandomMutationFor(Object, e => !e.IsDefect());
                    if (newMutation != null)
                    {
                        LL.Info($"[InstantMutating] {Object.DisplayName} gains mutation: {newMutation.DisplayName}", LogCategory.Debug);
                        Object.PlayWorldSound("Sounds/Misc/sfx_characterMod_mutation_positive");
                        MutationsAPI.ApplyMutationTo(Object, newMutation);
                    }
                    break;

                case "Defect":
                    MutationEntry newDefect = MutationsAPI.FindRandomMutationFor(Object, e => e.IsDefect(), allowMultipleDefects: true);
                    if (newDefect != null)
                    {
                        LL.Info($"[InstantMutating] {Object.DisplayName} gains defect: {newDefect.DisplayName}", LogCategory.Debug);
                        Object.PlayWorldSound("Sounds/Misc/sfx_characterMod_mutation_negative");
                        MutationsAPI.ApplyMutationTo(Object, newDefect);
                    }
                    break;
            }
        }
        protected void boostRandomStats(GameObject entity, int chance)
        {
            bool willGetBoost = Stat.Random(1, 100) <= chance;
            if (willGetBoost)
                StatUtils.BoostAttributes(entity, 6, 8);
                boostLevelHp(entity);
        }
        protected void boostRandomResists(GameObject entity, int chance)
        {
            bool willGetBoost = Stat.Random(1, 100) <= chance;
            if (willGetBoost)
                StatUtils.BoostResistances(entity, 10, 30);
                boostLevelHp(entity);
        }
        protected void boostLevelHp(GameObject entity)
        {
            bool willGetBoost = Stat.Random(1, 100) <= 80;
            if (willGetBoost)
            {
                StatUtils.BoostXPValue(entity, 400, 500);
                StatUtils.BoostEntityHealth(entity, 180, 240);
                StatUtils.BoostEntityLevel(entity, 6, 8);
            }
        }
    }
}
