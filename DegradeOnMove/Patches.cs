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
            if (!NoFoodLoSSMod.UseMod.Value) return value;

            if (Player.m_localPlayer.m_moveDir == Vector3.zero && Player.m_localPlayer.GetStamina() >= Player.m_localPlayer.GetMaxStamina())
            {
                return 0f;
            }

            return value;
        }
        
    }
}