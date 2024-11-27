using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExperimentalEnemyInteractions.EnemyPatches
{
    class PufferData
    {
        public int reactionToHit = 0;
        public EnemyAI? targetEnemy = null;
    }

    [HarmonyPatch(typeof(PufferAI))]
    class PufferAIPatch
    {
        static bool enableSporeLizard = Script.BoundingConfig.enableSporeLizard.Value;

        static Dictionary<PufferAI, PufferData> pufferList = [];

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPostfix(PufferAI __instance)
        {
            if (!pufferList.ContainsKey(__instance))
            {
                pufferList.Add(__instance, new PufferData());
            }
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPrefix]
        static bool PrefixAIInterval(PufferAI __instance)
        {
            PufferData pufferData = pufferList[__instance];

            if (__instance.currentBehaviourStateIndex == 2 && pufferData.targetEnemy != null && (Vector3.Distance(__instance.closestSeenPlayer.transform.position, __instance.transform.position) < Vector3.Distance(pufferData.targetEnemy.transform.position, __instance.transform.position)))
            {
                if (__instance.moveTowardsDestination)
                {
                    __instance.agent.SetDestination(__instance.destination);
                }
                __instance.SyncPositionToClients();
                return false;
            }
            return true;
        }

        [HarmonyPatch("DoAIInterval")]
        [HarmonyPostfix]
        static void PostfixAIInterval(PufferAI __instance)
        {
            PufferData pufferData = pufferList[__instance];

            if (__instance.currentBehaviourStateIndex == 2 && pufferData.targetEnemy != null && (Vector3.Distance(__instance.closestSeenPlayer.transform.position, __instance.transform.position) < Vector3.Distance(pufferData.targetEnemy.transform.position, __instance.transform.position)))
            {
                __instance.SetDestinationToPosition(pufferData.targetEnemy.transform.position, checkForPath: true);
            }
            else
            {
                pufferData.reactionToHit = 0;
            }
        }

        public static void CustomOnHit(int force, EnemyAI enemyWhoHit, bool playHitSFX, PufferAI instance)
        {
                if (enableSporeLizard != true) return;
                PufferData pufferData = pufferList[instance];
                instance.creatureAnimator.SetBool("alerted", true);
                instance.enemyHP -= force;
                Script.Logger.LogDebug("SpodeLizard CustomHit Triggered");
                HitEnemyTest(force, enemyWhoHit, playHitSFX, instance);
                instance.SwitchToBehaviourState(2);
                if (instance.enemyHP <= 0)
                {
                    instance.KillEnemy(true);
                }
        }

        public static void HitEnemyTest(int force, EnemyAI enemyWhoHit, bool playHitSFX, PufferAI instance)
        {
            int reactionINT = EnemyAIPatch.ReactToHit(force);

            if (enemyWhoHit is SandSpiderAI)
            {
                pufferList[instance].reactionToHit = 2;
            }
            else
            {
                pufferList[instance].reactionToHit = 1;
            }
        }
    }
}