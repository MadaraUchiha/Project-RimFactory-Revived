﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Threading.Tasks;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    
    public interface IAssemblerQueue
    {
        List<Thing> GetThingQueue();
    }


    //Art & maybe other things too need a seperate patch
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    class Patch_CountProducts_AssemblerQueue
    {

        static void Postfix(RecipeWorkerCounter __instance,ref int __result, Bill_Production bill)
        {
            if (bill.includeFromZone == null) {
                int i = 0;
                ThingDef targetDef = __instance.recipe.products[0].thingDef;


                for (i = 0; i < PRFGameComponent.AssemblerQueue.Count; i++)
                {
                    foreach (Thing heldThing in PRFGameComponent.AssemblerQueue[i].GetThingQueue())
                    {
                        Thing innerIfMinified = heldThing.GetInnerIfMinified();
                        if (innerIfMinified.def == targetDef)
                        {

                            __result += innerIfMinified.stackCount;
                        }
                    }
                }
            }
        }
    }



    [HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
    class Patch_UpdateResourceCounts_AssemblerQueue
    {

        static void Postfix(ResourceCounter __instance, Dictionary<ThingDef, int> ___countedAmounts )
        {
            int i = 0;
            for (i = 0; i < PRFGameComponent.AssemblerQueue.Count; i++)
            {
                foreach (Thing heldThing in PRFGameComponent.AssemblerQueue[i].GetThingQueue())
                {
                    Thing innerIfMinified = heldThing.GetInnerIfMinified();
                    //Added Should Count Checks
                    //EverStorable is form HeldThings
                    //Fresh Check is from ShouldCount (maybe we can hit that via harmony/reflection somhow)
                    if (innerIfMinified.def.EverStorable(false) && !innerIfMinified.IsNotFresh())
                    {
                        
                        ___countedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                    }
                    
                }
            }
        }
    }




}
