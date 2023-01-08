using System;
using System.Xml.Schema;
using UnityEngine;

namespace CW
{
    
// class needs to be on same level as animator to be able to activate animation events for the character and fire animation events
public class PlayerAttacker : MonoBehaviour
{
    private PlayerAnimatorManager playerAnimatorManager;
    private PlayerManager playerManager;
    private PlayerStats playerStats;
    public PlayerInventory playerInventory;
    private InputHandler inputHandler;
    private WeaponSlotManager weaponSlotManager;
    public string lastAttack;

    // its on layer 12, search on layer 12
    [SerializeField] private LayerMask backstabLayer = 1 << 12;

    public void Awake()
    {
        playerAnimatorManager = GetComponent<PlayerAnimatorManager>();
        playerManager = GetComponentInParent<PlayerManager>();
        playerStats = GetComponentInParent<PlayerStats>();
        playerInventory = GetComponentInParent<PlayerInventory>();
        inputHandler = GetComponentInParent<InputHandler>();
        weaponSlotManager = GetComponent<WeaponSlotManager>();
    }

    public void Update()
    {
        Debug.DrawRay(inputHandler.criticalAttackRaycatStartPoint.position, transform.TransformDirection(Vector3.forward) * 2, Color.cyan, 1f, false);
    }

    public void HandleWeaponCombo(WeaponItem weapon)
    {
        if (inputHandler.comboFlag)
        {
            playerAnimatorManager.anim.SetBool("canDoCombo", false);
            
            // play associated combo depending on previous attack
            if (lastAttack == weapon.OH_Light_Attack_1)
            {
                playerAnimatorManager.PlayTargetAnimation(weapon.OH_Light_Attack_2, true);
            }
            else if (lastAttack == weapon.th_light_attack_01)
            {
                playerAnimatorManager.PlayTargetAnimation(weapon.th_light_attack_02, true);
                lastAttack = weapon.th_light_attack_02;
            }
            else if (lastAttack == weapon.th_light_attack_02)
            {
                playerAnimatorManager.PlayTargetAnimation(weapon.th_heavy_attack_01, true);
                lastAttack = weapon.th_heavy_attack_01;
            }
        }
    }

    public void HandleLightAttack(WeaponItem weapon)
    {
        // assign attacking weapon regardless
        weaponSlotManager.attackingWeapon = weapon;
        // two hand attack
        if (inputHandler.twoHandFlag)
        {
            playerAnimatorManager.PlayTargetAnimation(weapon.th_light_attack_01, true);
            lastAttack = weapon.th_light_attack_01;
        }
        // one hand attack
        else
        {
            playerAnimatorManager.PlayTargetAnimation(weapon.OH_Light_Attack_1, true);
            lastAttack = weapon.OH_Light_Attack_1;
        }
       
    }
    
    public void HandleHeavyAttack(WeaponItem weapon)
    {
        // two hand attack
        if (inputHandler.twoHandFlag)
        {
            playerAnimatorManager.PlayTargetAnimation(weapon.th_heavy_attack_01, true);
            lastAttack = weapon.th_heavy_attack_01;
        } 
        // one hand attack
        else
        {
            weaponSlotManager.attackingWeapon = weapon;
            playerAnimatorManager.PlayTargetAnimation(weapon.OH_Heavy_Attack_1, true);
            lastAttack = weapon.OH_Heavy_Attack_1;
        }
    }


    #region Input Actions
    
    public void HandleRBAction()
    {
        // handle melee weapon attack
        if (playerInventory.rightWeapon.isMeleeWeapon)
        {
            PerformRBMeleeAction();
        }
        // handle spell action, handle miracle action, handle pyro action
        else if (playerInventory.rightWeapon.isSpellCaster 
                 || playerInventory.rightWeapon.isFaithCaster 
                 || playerInventory.rightWeapon.isPyroCaster)
        {
            PerformRBMagicAction(playerInventory.rightWeapon);
        }
        
    }
    #endregion

    #region Attack Actions

    private void PerformRBMeleeAction()
    {
        if (playerManager.canDoCombo)
        {
            inputHandler.comboFlag = true;
            inputHandler.comboFlag = false;
        }
        else
        {
            // unable to combo if player is interacting
            if (playerManager.isInteracting)
            {
                return;
            }
            if (playerManager.canDoCombo)
            {
                return;
            }
            playerAnimatorManager.anim.SetBool("isUsingRightHand", true);
            HandleLightAttack(playerInventory.rightWeapon);
        }
    }

    private void PerformRBMagicAction(WeaponItem weapon)
    {
        // break if player is interacting, prevents spam casting of spell
        if (playerManager.isInteracting)
        {
            return;
        }
        // check for type of spell being cast
        if (weapon.isFaithCaster)
        {
            if (playerInventory.currentSpell != null && playerInventory.currentSpell.isFaithSpell)
            {
                // check for focus point
                if (playerStats.currentMagic >= playerInventory.currentSpell.magicCost){
                    // attempt to cast spell
                    playerInventory.currentSpell.AttemptToCastSpell(playerAnimatorManager, playerStats);
                }
                // play an alternate out of magic animation
                else
                {
                    playerAnimatorManager.PlayTargetAnimation("Shrugging",true);
                }
            }
        }
    }

    // having a successfully cast spell here allows for
    // animation to be called as an animation event, same level as model, now can chose wichi frame of aniatio  to cast spell
    private void SuccessfullyCastSpell()
    {
        playerInventory.currentSpell.SuccessfullyCastSpell(playerAnimatorManager, playerStats);
    }
    
    #endregion

    public void AttemptBackstabOrRiposte()
    {
        // shoot raycast out of player when holding control
        RaycastHit hit;
        
        Debug.Log("attempting critical");
        if (Physics.Raycast(inputHandler.criticalAttackRaycatStartPoint.position,
                transform.TransformDirection(Vector3.forward), out hit, 2f, backstabLayer))
        {
            CharacterManager enemyCharacterManager = hit.transform.gameObject.GetComponentInParent<CharacterManager>();
            //damage logic for critical
            DamageCollider rightWeapon = weaponSlotManager.rightHandDamageCollider;
            
            // if found a enemy with a character manager
            if (enemyCharacterManager != null)
            {
                // check for team ID so you cant backstab allies or self
                // pull player into a transform behind enemy so backstab animation is clean
                // TODO: can use lerp to make transition smoother
                playerManager.transform.position = enemyCharacterManager.backstabCollider.backstabberStandPosition.position;
                // rotate player toards the transform
                Vector3 rotationDirection = playerManager.transform.eulerAngles;
                rotationDirection = hit.transform.position - playerManager.transform.position;
                rotationDirection.Normalize();
                Quaternion tr = Quaternion.LookRotation(rotationDirection);
                Quaternion targetRotation = Quaternion.Slerp(playerManager.transform.rotation, tr, 500 * Time.deltaTime);
                playerManager.transform.rotation = targetRotation;

                // having these variables separated allows for buffs and other modification later
                int criticalDamage = playerInventory.rightWeapon.criticalDamageMultiplier *
                                     rightWeapon.currentWeaponDamage;
                // assign damage to enemy
                enemyCharacterManager.pendingCriticalDamage = criticalDamage;
                
                // make enemy play animatiom
                playerAnimatorManager.PlayTargetAnimation("Backstab", true);
                enemyCharacterManager.GetComponentInChildren<AnimatorManager>().PlayTargetAnimation("Backstabbed", true);
                
                // do damage

            }
        }
    }
}
}
