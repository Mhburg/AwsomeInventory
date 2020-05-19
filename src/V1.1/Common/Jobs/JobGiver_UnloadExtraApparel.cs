﻿// <copyright file="JobGiver_UnloadExtraApparel.cs" company="Zizhen Li">
// Copyright (c) 2019 - 2020 Zizhen Li. All rights reserved.
// Licensed under the LGPL-3.0-only license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwesomeInventory.Loadout;
using RimWorld;
using Verse;
using Verse.AI;

namespace AwesomeInventory.Jobs
{
    /// <summary>
    /// Unload extra apparel in pawn's inventory.
    /// </summary>
    public class JobGiver_UnloadExtraApparel : ThinkNode
    {
        /// <inheritdoc/>
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (pawn?.inventory?.innerContainer == null)
                return ThinkResult.NoJob;

            if (!pawn.UseLoadout(out CompAwesomeInventoryLoadout comp))
                return ThinkResult.NoJob;

            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing is Apparel apparel)
                {
                    IEnumerable<ThingGroupSelector> selectors = comp.Loadout.Where(selector => selector.Allows(thing, out _));
                    if (selectors.Any())
                    {
                        foreach (ThingGroupSelector selector in selectors)
                        {
                            if (comp.InventoryMargins[selector] > 0)
                            {
                                return new ThinkResult(JobMaker.MakeJob(AwesomeInventory_JobDefOf.AwesomeInventory_Unload, thing), this, JobTag.UnloadingOwnInventory);
                            }
                        }
                    }
                    else
                    {
                        return new ThinkResult(JobMaker.MakeJob(AwesomeInventory_JobDefOf.AwesomeInventory_Unload, thing), this, JobTag.UnloadingOwnInventory);
                    }
                }
            }

            return ThinkResult.NoJob;
        }
    }
}
