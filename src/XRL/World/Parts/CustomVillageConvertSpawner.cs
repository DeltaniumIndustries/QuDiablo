using System;
using Qud.API;
using XRL;
using XRL.Wish;
using XRL.World;
using XRL.World.Parts;

[Serializable]
[HasWishCommand]
public class CustomVillageConvertSpawner : IPart
{
    public string Faction = "Mechanimists";
    public bool DoesWander = true;
    public bool IsPilgrim;

    public override bool SameAs(IPart p) => true;

    public override bool WantEvent(int ID, int cascade)
    {
        return base.WantEvent(ID, cascade) || ID == BeforeObjectCreatedEvent.ID;
    }

    public override bool HandleEvent(BeforeObjectCreatedEvent E)
    {
        GameObject gameObject = CreateFactionConvert(Faction, DoesWander, IsPilgrim, ParentObject.HasTag("IsLibrarian"));
        gameObject.FireEvent("VillageInit");
        gameObject.SetIntProperty("Social", 1);
        gameObject.SetStringProperty("SpawnedFrom", ParentObject.Blueprint);
        E.ReplacementObject = gameObject;
        return base.HandleEvent(E);
    }

    private static GameObject CreateFactionConvert(string faction, bool doesWander, bool isPilgrim = false, bool isLibrarian = false)
    {
        GameObject gameObject = GetBaseObjectByFaction(faction, isLibrarian);

        SetupAllegiance(gameObject, faction);
        SetupWanderingBehavior(gameObject, doesWander);
        SetupDialogue(gameObject, faction, isPilgrim);

        if (faction == "Mechanimists")
        {
            gameObject.ReceiveObject("Canticles3");
            if (isLibrarian)
            {
                gameObject.AddPart(new MechanimistLibrarian());
                gameObject.SetStringProperty("Mayor", "Mechanimists");
            }
            else
            {
                gameObject.RequirePart<SocialRoles>().RequireRole("Mechanimist convert");
            }
        }
        else if (faction == "Tristram")
        {
            gameObject.ReceiveObject("Grave Goods");
            gameObject.ReceiveObject("Plump Mushroom", RandomUtils.NextInt(2, 5));
            gameObject.RequirePart<SocialRoles>().RequireRole("Wanderer");
        }
        else if (faction == "Joppa")
        {
            gameObject.ReceiveObject("Vinewafer", RandomUtils.NextInt(0, 6));
            gameObject.ReceiveObject("Watervine", RandomUtils.NextInt(2, 10));
            gameObject.ReceiveObject("Starapple", RandomUtils.NextInt(0, 3));
            gameObject.RequirePart<SocialRoles>().RequireRole("citizen of Joppa");
        }

        return gameObject;
    }

    private static void SetupAllegiance(GameObject obj, string faction)
    {
        obj.Brain.Allegiance.Clear();
        obj.Brain.Allegiance.Add(faction, 100);
        obj.Brain.Allegiance.Hostile = false;
    }

    private static void SetupWanderingBehavior(GameObject obj, bool doesWander)
    {
        if (doesWander)
        {
            obj.Brain.Wanders = true;
            obj.Brain.WandersRandomly = true;
            obj.AddPart(new AIShopper());
        }
        else
        {
            obj.Brain.Wanders = false;
            obj.Brain.WandersRandomly = false;
            obj.AddPart(new AISitting());
        }
    }

    private static void SetupDialogue(GameObject obj, string faction, bool isPilgrim)
    {
        ConversationScript part = obj.GetPart<ConversationScript>();
        if (part == null) return;

        if (faction == "Mechanimists")
        {
            if (isPilgrim)
            {
                obj.RequirePart<AIPilgrim>();
                part.Append = "\n\nGlory to Shekhinah.~\n\nHumble before my Fathers, I walk.~\n\nShow mercy to a weary pilgrim.~\n\nPraise be upon Nisroch, who shelters us stiltseekers.";
            }
            else
            {
                obj.RemovePart<AIPilgrim>();
                part.Append = "\n\nGlory to Shekhinah.~\n\nMay the ground shake but the Six Day Stilt never tumble!~\n\nPraise our argent Fathers! Wisest of all beings.";
            }
        }
        else if (faction == "Tristram")
        {
            part.Append = "\n\nThe road whispers beneath weary boots.~\n\nSeek him where the lanterns flicker low.~\n\nCome close, and weave a tale for the embers.~\n\nThe earth clutches its secrets in gnarled roots.~\n\nStand still as the grave, and ponder.~\n\nWhat will you offer to the stranger’s fire?";
        }
        else if (faction == "Joppa")
        {
            part.Append = "The watervine grows thick this year. Good harvest ahead.";
        }
    }

    private static GameObject GetBaseObjectByFaction(string faction, bool isLibrarian = false)
    {
        if (faction == "Tristram") return GetBaseObject_Tristram();
        if (faction == "Joppa") return GetBaseObject_Joppa();
        return GetBaseObject(isLibrarian);
    }

    private static GameObject GetBaseObject(bool isLibrarian = false)
    {
        return isLibrarian
            ? EncountersAPI.GetALegendaryEligibleCreatureWithAnInventory(o => !o.HasTag("NoLibrarian") && !o.HasTag("ExcludeFromVillagePopulations"))
            : EncountersAPI.GetALegendaryEligibleCreature(o => !o.HasTag("ExcludeFromVillagePopulations"));
    }

    private static GameObject GetBaseObject_Tristram()
    {
        return EncountersAPI.GetALegendaryEligibleCreature(o => !o.HasTag("ExcludeFromVillagePopulations") && o.HasTag("Humanoid") && !o.InheritsFrom("Goatfolk"));
    }

    private static GameObject GetBaseObject_Joppa()
    {
        int roll = RandomUtils.NextInt(1, 100);
        if (roll <= 40)
            return EncountersAPI.GetALegendaryEligibleCreature(o => !o.HasTag("ExcludeFromVillagePopulations") && o.HasTag("DynamicObjectsTable:Jungle_Creatures") && !o.InheritsFrom("Goatfolk"));
        if (roll <= 50)
            return EncountersAPI.GetALegendaryEligibleCreature(o => !o.HasTag("ExcludeFromVillagePopulations") && o.HasTag("DynamicObjectsTable:Jungle_Creatures"));
        if (roll <= 65)
            return EncountersAPI.GetALegendaryEligibleCreature(o => !o.HasTag("ExcludeFromVillagePopulations") && o.HasTag("Humanoid") && !o.InheritsFrom("Goatfolk"));
        return EncountersAPI.GetALegendaryEligibleCreature(o => !o.HasTag("ExcludeFromVillagePopulations"));
    }

    [WishCommand("convertLaurus", null)]
    public static void Wish(string Param)
    {
        Param.Split(':', out var First, out var Second);
        if (Second.IsNullOrEmpty()) { Second = First; First = "mechanimist"; }

        WishResult wishResult = WishSearcher.SearchForBlueprint(Second);
        GameObject parentObject = GameObjectFactory.Factory.CreateObject(wishResult.Result, 0, 0, null, null, null, "Wish");

        parentObject = CreateFactionConvert(First, true);
        parentObject.FireEvent("VillageInit");
        The.PlayerCell.getClosestEmptyCell().AddObject(parentObject);
    }
}
