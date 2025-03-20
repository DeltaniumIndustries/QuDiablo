using System;
using System.Collections.Generic;
using XRL.Rules;
using XRL.UI;
using XRL.World;

namespace XRL.World.Parts
{
    [Serializable]
    public abstract class LaurusStairsDown : StairsDown
    {
        // Expose the key fields
        public bool IsConnected => Connected;
        public string ConnectionObjectName => ConnectionObject;
        public bool IsPullDown => PullDown;
        public bool IsGenericFall => GenericFall;
        public bool ShouldConnectLanding => ConnectLanding;
        public string FallMessage => PullMessage;
        public string DescendPrompt => JumpPrompt;
        public string FallSound => Sound;
        public int FallLevels => Levels;

        public override void Register(GameObject obj, IEventRegistrar registrar)
        {
            base.Register(obj, registrar);
            RegisterAdditionalEvents(obj, registrar);
        }

        protected virtual void RegisterAdditionalEvents(GameObject obj, IEventRegistrar registrar)
        {
            // Allow subclasses to register custom events
        }

        public override bool FireEvent(Event e)
        {
            if (HandleCustomEvent(e))
                return true;
            return base.FireEvent(e);
        }

        protected virtual bool HandleCustomEvent(Event e)
        {
            return false; // Allow subclasses to override
        }

        protected new virtual bool IsValidForPullDown(GameObject obj)
        {
            return GameObject.Validate(ref obj) &&
                   obj != this.ParentObject &&
                   obj.CanFall;
        }

        public new bool CheckPullDown(GameObject Object)
        {
            try
            {
                if (!CanPullDown(Object))
                {
                    return false;
                }


                Cell cell = ParentObject.CurrentCell;
                var (DestinationCell, Distance) = CalculateDestinationCell(ParentObject);
                if (DestinationCell == null)
                {
                    return false;
                }

                HandlePullDownEvent(Object, Distance);
                MoveObjectToCell(Object, DestinationCell);
                InflictFallDamageIfNeeded(Object, DestinationCell, Distance);
                HandleZoneTransition(Object, DestinationCell, Distance);
                return true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException("StairsDown::CheckPulldown", x);
                return false;
            }
        }

        public bool CanPullDown(GameObject Object)
        {
            if (!PullDown || !GameObject.Validate(ParentObject) || !IsValidForPullDown(Object))
            {
                return false;
            }

            return true;
        }

        private (Cell DestinationCell, int Distance) CalculateDestinationCell(GameObject Object)
        {
            Cell cell = ParentObject.CurrentCell;
            Cell DestinationCell = GetPullDownCell(cell, out var Distance);

            if (DestinationCell == null || !BeforePullDownEvent.Check(ParentObject, Object, ref DestinationCell))
            {
                return (null, 0);
            }

            AdjustDestinationCellForPassability(Object, ref DestinationCell);
            return (DestinationCell, Distance);
        }


        private void AdjustDestinationCellForPassability(GameObject Object, ref Cell DestinationCell)
        {
            if (!DestinationCell.IsPassable(Object))
            {
                Cell closestPassableCellFor = DestinationCell.getClosestPassableCellFor(Object);
                if (closestPassableCellFor != null && closestPassableCellFor.RealDistanceTo(DestinationCell) <= 2.0)
                {
                    DestinationCell = closestPassableCellFor;
                }
            }
        }

        private void HandlePullDownEvent(GameObject Object, int Distance)
        {
            PlayFallSound(Distance);
            if (Object.IsPlayerLed() && !Object.IsTrifling)
            {
                DisplayCompanionFallMessage(Object);
            }
            else
            {
                LogFallEvent(Object);
            }
        }

        private void PlayFallSound(int Distance)
        {
            string fallSound = Distance > 1 ? "sfx_characterTrigger_shaft_fall" : "fly_generic_fall";
            PlayWorldSound(fallSound);
        }

        private void DisplayCompanionFallMessage(GameObject Object)
        {
            string text = GetFallPreposition();
            bool useDefiniteArticle = ShouldUseDefiniteArticle();
            string companionMessage = $"{Object.GetDisplayName(int.MaxValue)} has fallen {text} {ParentObject.t()} {The.Player.DescribeDirectionToward(ParentObject)}!";
            Popup.Show(companionMessage);
        }

        private string GetFallPreposition()
        {
            return ParentObject.HasPropertyOrTag("FallPreposition") ? ParentObject.GetPropertyOrTag("FallPreposition") : "down";
        }

        private bool ShouldUseDefiniteArticle()
        {
            return ParentObject.HasPropertyOrTag("FallUseDefiniteArticle") && ParentObject.GetPropertyOrTag("FallUseDefiniteArticle") == "true";
        }

        private void LogFallEvent(GameObject Object)
        {
            XDidYToZ(Object, "fall", GetFallPreposition(), ParentObject, null, null, null, null, null, Object, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: false);
        }

        private void MoveObjectToCell(GameObject Object, Cell DestinationCell)
        {
            Object.SystemMoveTo(DestinationCell, 0, forced: true);
            if (!Object.IsPlayer())
            {
                LogFallDamageFromAbove(Object);
            }
        }

        private void LogFallDamageFromAbove(GameObject Object)
        {
            XDidY(Object, "fall", "down from above", null, null, null, null, Object, UseFullNames: false, IndefiniteSubject: true);
        }

        private void InflictFallDamageIfNeeded(GameObject Object, Cell DestinationCell, int Distance)
        {
            List<GameObject> affectedObjects = GetObjectsAffectedByFall(Object, DestinationCell);
            foreach (GameObject Obj in affectedObjects)
            {
                InflictFallDamage(Obj, Distance);
            }
        }

        private List<GameObject> GetObjectsAffectedByFall(GameObject Object, Cell DestinationCell)
        {
            List<GameObject> affectedObjects = null;
            if (Object.GetMatterPhase() <= 2)
            {
                int phase = Object.GetPhase();
                foreach (var gameObject in DestinationCell.Objects)
                {
                    if (gameObject != null && gameObject != Object && gameObject.HasPart<Combat>() && gameObject.GetMatterPhase() <= 2 && gameObject.PhaseMatches(phase))
                    {
                        if (affectedObjects == null)
                        {
                            affectedObjects = Event.NewGameObjectList();
                        }
                        affectedObjects.Add(gameObject);
                    }
                }
            }
            return affectedObjects;
        }

        private void HandleZoneTransition(GameObject Object, Cell DestinationCell, int Distance)
        {
            if (Object.IsPlayer())
            {
                The.ZoneManager.SetActiveZone(DestinationCell.ParentZone);
                The.ZoneManager.ProcessGoToPartyLeader();
                if (Distance > 1)
                {
                    AddPlayerMessage("You fall downward!");
                }
                else if (!PullMessage.IsNullOrEmpty())
                {
                    AddPlayerMessage(PullMessage);
                }
            }

            AdjustPlayerPositionIfSolid(Object);
        }

        private void AdjustPlayerPositionIfSolid(GameObject Object)
        {
            if ((Object?.CurrentCell?.IsSolid()).GetValueOrDefault())
            {
                Cell cell2 = Object?.CurrentCell?.GetFirstPassableConnectedAdjacentCell();
                if (cell2 != null)
                {
                    Object.SystemMoveTo(cell2);
                }
            }
        }


        protected virtual void InflictFallDamage(GameObject obj, int distance)
        {
            if (distance <= 1) return;

            string deathReason = $"You fell from a height of {distance} levels.";
            string thirdPersonDeathReason = $"{obj.It} fell from a height of {distance} levels.";

            int damage = distance * 15 + 20;
            obj.TakeDamage(damage, "falling", "Crushing", DeathReason: deathReason, ThirdPersonDeathReason: thirdPersonDeathReason, obj, Accidental: true);
            if (obj.IsPlayer() && obj.hitpoints <= 0)
            {
                Achievement.DIE_BY_FALLING.Unlock();
            }
        }

    }
}
