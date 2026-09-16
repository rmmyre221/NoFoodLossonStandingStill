using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace NoFoodLoSS
{
    public class PatchClass
    {
        /*
         IL_0000: ldarg.0      // this
        IL_0001: ldarg.0      // this
        IL_0002: ldfld        float32 Player::m_foodUpdateTimer
        IL_0007: ldarg.1      // dt
        IL_0008: add
        IL_0009: stfld        float32 Player::m_foodUpdateTimer

         */

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
        public class FoodDegredationTranspiler
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var field = AccessTools.Field(typeof(Player), nameof(Player.m_foodUpdateTimer));
                return new CodeMatcher(instructions)
                    .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldfld, field))
                    .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldarg_1))
                    .Advance(1)
                    .InsertAndAdvance(Transpilers.EmitDelegate<Func<float, float>>(CheckPlayerMovementSetFood))
                    .InstructionEnumeration();
            }
        }


        public static float CheckPlayerMovementSetFood(float value)
        {
            if (!NoFoodLoSSMod.UseMod.Value)
            {
                return value;
            }
            return value * Math.Max(GenerateMultiplier() * RestedMultiplier(), NoFoodLoSSMod.MinimumHungerLoss.Value);
        }
        public static float GenerateMultiplier()
        {
            float multiplier = NoFoodLoSSMod.BaseHungerLoss.Value;
            Player localPlayer = Player.m_localPlayer;
            bool isMoving = ((Character)localPlayer).m_moveDir != Vector3.zero;
            if (NoFoodLoSSMod.PartialLossMode.Value)
            {
                int num = 0;
                if (NoFoodLoSSMod.RequireStandingStillInPartialMode.Value)
                {
                    if (isMoving)
                    {
                        return multiplier;
                    }
                }
                else if (!isMoving)
                {
                    num += NoFoodLoSSMod.MovementWeight.Value;
                }
                if (localPlayer.GetHealth() >= localPlayer.GetMaxHealth())
                {
                    num += NoFoodLoSSMod.HealthWeight.Value;
                }
                if (localPlayer.GetStamina() >= localPlayer.GetMaxStamina())
                {
                    num += NoFoodLoSSMod.StaminaWeight.Value;
                }
                if (localPlayer.GetEitr() >= localPlayer.GetMaxEitr())
                {
                    num += NoFoodLoSSMod.EitrWeight.Value;
                }
                return multiplier * (1f - (float)num * NoFoodLoSSMod.CachedInverseTotalWeight);
            }
            if (NoFoodLoSSMod.CheckMovement.Value && isMoving)
            {
                return multiplier;
            }
            if (NoFoodLoSSMod.CheckHealth.Value && localPlayer.GetHealth() < localPlayer.GetMaxHealth())
            {
                return multiplier;
            }
            if (NoFoodLoSSMod.CheckStamina.Value && localPlayer.GetStamina() < localPlayer.GetMaxStamina())
            {
                return multiplier;
            }
            if (NoFoodLoSSMod.CheckEitr.Value && localPlayer.GetEitr() < localPlayer.GetMaxEitr())
            {
                return multiplier;
            }
            return 0f;
        }
        public static float RestedMultiplier()
        {
            if (!NoFoodLoSSMod.UseRestedBonus.Value || !Player.m_localPlayer.GetSEMan().HaveStatusEffect("Rested".GetStableHashCode()))
            {
                return 1f;
            }
            return NoFoodLoSSMod.RestedMultiplier.Value;
        }
    }
}