﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;
using UnityEngine;

namespace RPG_Inventory_Remake.RPGLoadout
{
    public class CompInventory : ThingComp
    {
        #region Fields

        private Pawn parentPawnInt = null;
        private const int CLEANUPTICKINTERVAL = GenTicks.TickLongInterval;
        private int ticksToNextCleanUp = GenTicks.TicksAbs;
        private float currentWeightCached;
        private List<Thing> ammoListCached = new List<Thing>();
        private List<ThingWithComps> meleeWeaponListCached = new List<ThingWithComps>();
        private List<ThingWithComps> rangedWeaponListCached = new List<ThingWithComps>();

        #endregion

        #region Properties

        public CompProperties_Inventory Props
        {
            get
            {
                return (CompProperties_Inventory)props;
            }
        }

        public float currentWeight
        {
            get
            {
                return currentWeightCached;
            }
        }

        private float availableWeight
        {
            get
            {
                return capacityWeight - currentWeight;
            }
        }


        public float capacityWeight
        {
            get
            {
                return parentPawn.GetStatValue(StatDefOf.Mass);
            }
        }
        private Pawn parentPawn
        {
            get
            {
                if (parentPawnInt == null)
                {
                    parentPawnInt = parent as Pawn;
                }
                return parentPawnInt;
            }
        }
        public float moveSpeedFactor
        {
            get
            {
                return MassBulkUtility.MoveSpeedFactor(currentWeight, capacityWeight);
            }
        }

        public float encumberPenalty
        {
            get
            {
                return MassBulkUtility.EncumberPenalty(currentWeight, capacityWeight);
            }
        }
        public ThingOwner container
        {
            get
            {
                if (parentPawn.inventory != null)
                {
                    return parentPawn.inventory.innerContainer;
                }
                return null;
            }
        }
        public List<Thing> ammoList => ammoListCached;
        public List<ThingWithComps> meleeWeaponList => meleeWeaponListCached;
        public List<ThingWithComps> rangedWeaponList => rangedWeaponListCached;

        #endregion Properties

        #region Methods

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            UpdateInventory();
        }

        /// <summary>
        /// Refreshes the cached and weight. Call this whenever items are added/removed from inventory
        /// </summary>
        public void UpdateInventory()
        {
            if (parentPawn == null)
            {
                Log.Error("CompInventory on non-pawn " + parent.ToString());
                return;
            }
            float newWeight = 0f;

            // Add equipped weapon
            if (parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                GetEquipmentStats(parentPawn.equipment.Primary, out newWeight);
            }

            // Add apparel
            if (parentPawn.apparel != null && parentPawn.apparel.WornApparelCount > 0)
            {
                foreach (Thing apparel in parentPawn.apparel.WornApparel)
                {
                    float apparelWeight = apparel.GetStatValue(StatDefOf.Mass);
                    newWeight += apparelWeight;
                    
                }
            }

            // Add inventory items
            if (parentPawn.inventory != null && parentPawn.inventory.innerContainer != null)
            {
                ammoListCached.Clear();
                meleeWeaponListCached.Clear();
                rangedWeaponListCached.Clear();

                List<HoldRecord> recs = LoadoutManager.GetHoldRecords(parentPawn);
                foreach (Thing thing in parentPawn.inventory.innerContainer)
                {
                    // Check for weapons
                    ThingWithComps eq = thing as ThingWithComps;
                    CompEquippable compEq = thing.TryGetComp<CompEquippable>();
                    if (eq != null && compEq != null)
                    {
                        if (eq.def.IsRangedWeapon)
                        {
                            rangedWeaponListCached.Add(eq);
                        }
                        else
                        {
                            meleeWeaponListCached.Add(eq);
                        }
                        // Calculate equipment weight
                        float eqWeight;
                        GetEquipmentStats(eq, out eqWeight);
                        newWeight += eqWeight * thing.stackCount;
                    }
                    else
                    {
                        // Add item weight
                        newWeight += thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
                    }
                    if (recs != null)
					{
                    	HoldRecord rec = recs.FirstOrDefault(hr => hr.thingDef == thing.def);
						if (rec != null && !rec.pickedUp)
							rec.pickedUp = true;
					}
                }
            }
            currentWeightCached = newWeight;
        }

        /// <summary>
        /// Determines if and how many of an item currently fit into the inventory with regards to weight/bulk constraints.
        /// </summary>
        /// <param name="thing">Thing to check</param>
        /// <param name="count">Maximum amount of that item that can fit into the inventory</param>
        /// <param name="ignoreEquipment">Whether to include currently equipped weapons when calculating current weight/bulk</param>
        /// <param name="useApparelCalculations">Whether to use calculations for worn apparel. This will factor in equipped stat offsets boosting inventory space and use the worn bulk and weight.</param>
        /// <returns>True if one or more items fit into the inventory</returns>
        public bool CanFitInInventory(Thing thing, out int count, bool ignoreEquipment = false, bool useApparelCalculations = false)
        {
            float thingWeight;

            if (useApparelCalculations)
            {
                thingWeight = thing.GetStatValue(StatDefOf.Mass);
                if (thingWeight <= 0)
                {
                    count = 1;
                    return true;
                }
                // Subtract the stat offsets we get from wearing this
                thingWeight -= thing.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Mass);
            }
            else
            {
                thingWeight = thing.GetStatValue(StatDefOf.Mass);
            }
            // Subtract weight of currently equipped weapon
            float eqWeight = 0f;
            if (ignoreEquipment && parentPawn.equipment != null && parentPawn.equipment.Primary != null)
            {
                ThingWithComps eq = parentPawn.equipment.Primary;
                GetEquipmentStats(eq, out eqWeight);
            }
            // Calculate how many items we can fit into our inventory
            float amountByWeight = thingWeight <= 0 ? thing.stackCount : (availableWeight + eqWeight) / thingWeight;
            count = Mathf.FloorToInt(Mathf.Min(amountByWeight, thing.stackCount));
            return count > 0;
        }

        public static void GetEquipmentStats(ThingWithComps eq, out float weight)
        {
                 weight = eq.GetStatValue(StatDefOf.Mass);
        }

        /// <summary>
        /// Attempts to equip a weapon from the inventory, puts currently equipped weapon into inventory if it exists
        /// </summary>
        /// <param name="useFists">Whether to put the currently equipped weapon away even if no replacement is found</param>
        public void SwitchToNextViableWeapon(bool useFists = true)
        {
            ThingWithComps newEq = null;

            // Stop current job
            if (parentPawn.jobs != null)
                parentPawn.jobs.StopAll();

            // If no ranged weapon was found, use first available melee weapons
            if (newEq == null)
                newEq = meleeWeaponListCached.FirstOrDefault();

            // Equip the weapon
            if (newEq != null)
            {
                TrySwitchToWeapon(newEq);
            }
            else if (useFists)
            {
                // Put away current weapon
                ThingWithComps eq = parentPawn.equipment?.Primary;
                if (eq != null && !parentPawn.equipment.TryTransferEquipmentToContainer(eq, container))
                {
                    // If we can't put it into our inventory, drop it
                    if (parentPawn.Position.InBounds(parentPawn.Map))
                    {
                        ThingWithComps unused;
                        parentPawn.equipment.TryDropEquipment(eq, out unused, parentPawn.Position);
                    }
                    else
                    {
#if DEBUG
                        Log.Warning("CE :: CompInventory :: SwitchToNextViableWeapon :: destroying out of bounds equipment" + eq.ToString());
#endif
                        if (!eq.Destroyed)
                        {
                            eq.Destroy();
                        }
                    }
                }
            }
        }

        public void TrySwitchToWeapon(ThingWithComps newEq)
        {
            if (newEq == null || parentPawn.equipment == null || !container.Contains(newEq))
            {
                return;
            }

            // Stop current job
            if (parentPawn.jobs != null)
                parentPawn.jobs.StopAll();

            if (parentPawn.equipment.Primary != null)
            {
                int count;
                if (CanFitInInventory(parentPawn.equipment.Primary, out count, true))
                {
                    parentPawn.equipment.TryTransferEquipmentToContainer(parentPawn.equipment.Primary, container);
                }
                else
                {
#if DEBUG
                    Log.Warning("CE :: CompInventory :: TrySwitchToWeapon :: failed to add current equipment to inventory");
#endif
                    parentPawn.equipment.MakeRoomFor(newEq);
                }
            }
            parentPawn.equipment.AddEquipment((ThingWithComps)container.Take(newEq, 1));
            if (newEq.def.soundInteract != null)
                newEq.def.soundInteract.PlayOneShot(new TargetInfo(parent.Position, parent.MapHeld, false));
        }

        public override void CompTick()
        {
            if (GenTicks.TicksAbs >= ticksToNextCleanUp)
            {
	            // Ask HoldTracker to clean itself up...
	            parentPawn.HoldTrackerCleanUp();
	            ticksToNextCleanUp = GenTicks.TicksAbs + CLEANUPTICKINTERVAL;
            }
            base.CompTick();

            //if (Controller.settings.DebugEnableInventoryValidation) ValidateCache();
        }

        /// <summary>
        /// Debug method to catch cases where inventory cache isn't being updated properly on pawn inventory change. ONLY FOR DEBUGGING, DON'T CALL THIS IN ANY KIND OF RELEASE BUILD.
        /// </summary>
        private void ValidateCache()
        {
            float oldWeight = currentWeight;
            UpdateInventory();
            if (oldWeight != currentWeight)
            {
                Log.Error("CE :: CompInventory :: " + parent.ToString() + " failed inventory validation");
            }
        }

        #endregion Methods
    }
}