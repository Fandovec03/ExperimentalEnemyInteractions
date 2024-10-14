﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ExperimentalEnemyInteractions.Patches
{
    class BeeValues
    {
        public EnemyAI? closestEnemy = null;
        public EnemyAI? targetEnemy = null;
        public EnemyAI? closestToBeehive = null;
        public GrabbableObject? hive = null;
        public Vector3 lastKnownHivePosition = Vector3.zero;
        public int customBehaviorStateIndex = 0;
        public float timeSinceHittingEnemy = 0f;
    }


    [HarmonyPatch(typeof(RedLocustBees))]
    class BeeAIPatch
    {
        static Dictionary<RedLocustBees, BeeValues> beeList = [];
        static List<EnemyAI> enemyList = new List<EnemyAI>();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch(RedLocustBees __instance)
        {
            beeList.Add(__instance, new BeeValues());
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool DoAIIntervalPrefixPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];

            if (beeData.targetEnemy != null)
            {
                return false;
            }
            else if (beeData.targetEnemy == null || beeData.customBehaviorStateIndex == 2)
            {
                return true;    
            }
            return true;
        }
        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void DoAIIntervalPostfixPatch(RedLocustBees __instance)
        {
            BeeValues beeData = beeList[__instance];

            enemyList = EnemyAIPatch.GetOutsideEnemyList(EnemyAIPatch.GetCompleteList(__instance.GetType()), __instance);

            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    EnemyAI enemyAI = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1);

                    Script.Logger.LogDebug("case0: Checked LOS for enemies. Enemy found: " + enemyAI);

                    if (enemyAI != null && Vector3.Distance(enemyAI.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                    {
                        __instance.SetDestinationToPosition(enemyAI.transform.position, true);
                        __instance.moveTowardsDestination = true;
                        Script.Logger.LogDebug("case0: Moving towards " + enemyAI);

                        beeData.customBehaviorStateIndex = 1;
                        __instance.SwitchToBehaviourServerRpc(1);
                        Script.Logger.LogDebug("case0: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        __instance.syncedLastKnownHivePosition = false;
                        __instance.SyncLastKnownHivePositionServerRpc(__instance.lastKnownHivePosition);
                    }
                    break;
                case 1:
                    if (beeData.targetEnemy == null || Vector3.Distance(beeData.targetEnemy.transform.position, __instance.hive.transform.position) > (float)__instance.defenseDistance + 5f)
                    {
                        if (__instance.IsHiveMissing())
                        {
                            beeData.customBehaviorStateIndex = 2;
                            __instance.SwitchToBehaviourServerRpc(2);
                            Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourServerRpc(0);
                            Script.Logger.LogDebug("case1: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                    }
                    else if (__instance.targetPlayer.currentlyHeldObject == __instance.hive)
                    {
                        beeData.customBehaviorStateIndex = 2;
                        __instance.SwitchToBehaviourServerRpc(2);
                    }
                    break;
                case 2: // Currently whenever bees go to state 2 they will ignore players and stop reporting into logs. Disabled for now
                    if (__instance.IsHivePlacedAndInLOS())
                    {
                        if (__instance.wasInChase)
                        {
                            Script.Logger.LogDebug("case2: set wasInChase to false");
                            __instance.wasInChase = false;
                        }
                        enemyAI = EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1);
                        Script.Logger.LogDebug("case2: checked LOS for enemies");
                        if (enemyAI != null && Vector3.Distance(enemyAI.transform.position, __instance.hive.transform.position) < (float)__instance.defenseDistance)
                        {
                            __instance.SetDestinationToPosition(enemyAI.transform.position, true);
                            __instance.moveTowardsDestination = true;
                            Script.Logger.LogDebug("case2: Moving towards: " + enemyAI);
                            beeData.customBehaviorStateIndex = 1;
                            __instance.SwitchToBehaviourServerRpc(1);
                            Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                        else
                        {
                            beeData.customBehaviorStateIndex = 0;
                            __instance.SwitchToBehaviourServerRpc(0);
                            Script.Logger.LogDebug("case2: CustomBehaviorStateIndex changed: " + beeData.customBehaviorStateIndex);
                        }
                    }
                    if (beeData.targetEnemy != null)
                    {
                        __instance.agent.acceleration = 16f;
                        if (!EnemyAIPatch.CheckLOSForEnemies(__instance, enemyList, 360f, 16, 1))
                        {
                            Script.Logger.LogDebug("case2: Checking LOS for enemies.");

                            __instance.lostLOSTimer += __instance.AIIntervalTime;
                            if (__instance.lostLOSTimer > 4.5f)
                            {
                                beeData.targetEnemy = null;
                                Script.Logger.LogDebug("case2: No target found.");
                                __instance.lostLOSTimer = 0;
                            }
                        }
                        else
                        {
                            __instance.wasInChase = true;
                            __instance.lostLOSTimer = 0;
                        }
                    }
                    __instance.agent.acceleration = 13f;
                    {
                        if (!__instance.searchForHive.inProgress)
                        {
                            __instance.StartSearch(__instance.transform.position, __instance.searchForHive);
                        }
                    }
                    break;
            }
        }

        public static void OnCustomEnemyCollision(RedLocustBees __instance, EnemyAI mainscript2)
        {
            if (beeList[__instance].timeSinceHittingEnemy > 1.6f)
            {
                mainscript2.HitEnemy(1, null, playHitSFX: true);
                beeList[__instance].timeSinceHittingEnemy = 0f;
            }
            else
            {
                beeList[__instance].timeSinceHittingEnemy += Time.deltaTime;
            }
        }
    }
}