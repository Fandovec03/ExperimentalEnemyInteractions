using HarmonyLib;
using NaturalSelection.EnemyPatches;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace NaturalSelection.Generics
{
    [HarmonyPatch(typeof(RoundManager))]
    class RoundManagerPatch
    {
        static public float nextUpdate = 0;
        static List<Type> checkedList = new List<Type>();

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdateRMPatch()
        {
            if (Time.realtimeSinceStartup >= nextUpdate)
            {
                checkedList.Clear();
            }
        }

        public static bool RequestUpdateList(EnemyAI instance)
        {
            if (!checkedList.Contains(instance.GetType()))
            {
                checkedList.Add(instance.GetType());
                //NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(enemyList);
                Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(instance) + " got true from RequestUpdateList");
                return true;//NaturalSelectionLib.NaturalSelectionLib.globalEnemyLists[instance.GetType()];
            }
            else
            {
                Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(instance) + " got false from RequestUpdateList");
                return false;
            }
        }
    }
}
