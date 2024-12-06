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
                nextUpdate = Time.realtimeSinceStartup + 1;
            }
        }

        public static void RequestUpdateList(EnemyAI instance, List<EnemyAI> list)
        {
            if (!checkedList.Contains(instance.GetType()))
            {
                checkedList.Add(instance.GetType());
                NaturalSelectionLib.NaturalSelectionLib.UpdateListInsideDictionrary(instance,list);
                Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(instance) + " got true from RequestUpdateList. " + Time.realtimeSinceStartup + ", next update: " + nextUpdate);
                //return true;
            }
            else
            {
                Script.Logger.LogDebug(EnemyAIPatch.DebugStringHead(instance) + " got false from RequestUpdateList " + Time.realtimeSinceStartup + ", next update: " + nextUpdate);
                //return false;
            }
        }
    }
}
