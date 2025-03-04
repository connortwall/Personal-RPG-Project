using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CW
{
    
   [CreateAssetMenu(menuName = "Spells/Healing Spell")]
public class HealingSpell : SpellItem
{

   public int healAmount;

   public override void AttemptToCastSpell(
      PlayerAnimatorManager playerAnimatorManager, 
      PlayerStatsManager playerStatsManager,
      PlayerWeaponSlotManager playerWeaponSlotManager)
   {
      base.AttemptToCastSpell(playerAnimatorManager, playerStatsManager, playerWeaponSlotManager);
      GameObject instantiateWarmUpSpellFX = Instantiate(spellWarmUpFX, playerAnimatorManager.transform);
      playerAnimatorManager.PlayTargetAnimation(spellAnimation, true);
      Debug.Log("Attempting to cast spell");
   }

   public override void SuccessfullyCastSpell(
      PlayerAnimatorManager playerAnimatorManager, 
      PlayerStatsManager playerStatsManager,
      CameraHandler cameraHandler,
      PlayerWeaponSlotManager playerWeaponSlotManager)
   {
      // also fires function in original spell item class
      base.SuccessfullyCastSpell(playerAnimatorManager,playerStatsManager, cameraHandler, playerWeaponSlotManager);
      GameObject instantiatedSpellFX = Instantiate(spellCastFX, playerAnimatorManager.transform);
      playerStatsManager.HealPlayer(healAmount);
      Debug.Log("Spell cast successful");
   }
}

}
