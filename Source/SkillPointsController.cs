using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using Rebindables;
using Rewired;
using RiskOfOptions;
using RoR2;
using SkillsPlusPlus.Modifiers;
using SkillsPlusPlus.Source;
using UnityEngine;
using UnityEngine.Networking;

namespace SkillsPlusPlus
{

    [RequireComponent(typeof(PlayerCharacterMasterController))]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class SkillPointsController : NetworkBehaviour
    {
        private PlayerCharacterMasterController playerCharacterMasterController;

        private CharacterBody body => playerCharacterMasterController.master?.GetBody();
        private SkillLocator skillLocator => body?.skillLocator;

        private TeamIndex PlayerTeamIndex => playerCharacterMasterController.master.hasBody ? playerCharacterMasterController.master.GetBody().teamComponent.teamIndex : TeamIndex.None;

        [SyncVar]
        public bool isSurvivorEnabled;

        [SyncVar]
        private int earnedSkillPoints;

        [SyncVar]
        private int unspentSkillPoints;

        [SyncVar]
        private int levelsPerSkillPoint = SkillOptions.levelsPerSkillPoint.Value;

        [SyncVar]
        public bool multScalingLinear;

        public bool hasUnspentPoints => unspentSkillPoints > 0;

        private Dictionary<string, int> transferrableSkillUpgrades = new Dictionary<string, int>();
        

        private void Awake()
        {
            multScalingLinear = SkillOptions.multScalingLinear.Value;
            Log.Debug($"levelsPerSkillPoint: {this.levelsPerSkillPoint}");

            this.playerCharacterMasterController = this.GetComponent<PlayerCharacterMasterController>();
        }

        private void OnEnable()
        {
            this.playerCharacterMasterController.master.onBodyStart += this.OnBodyStart;
            On.RoR2.CharacterMaster.GetDeployableSameSlotLimit += this.GetDeployableSameSlotLimit;
            On.EntityStates.GenericCharacterMain.CanExecuteSkill += this.GenericCharacterMain_CanExecuteSkill;
            if (NetworkServer.active)
            {
                On.RoR2.CharacterBody.RecalculateStats += this.OnRecalculateStats;
            }
        }

        private void OnDisable()
        {
            this.playerCharacterMasterController.master.onBodyStart -= this.OnBodyStart;
            On.RoR2.CharacterMaster.GetDeployableSameSlotLimit -= this.GetDeployableSameSlotLimit;
            On.EntityStates.GenericCharacterMain.CanExecuteSkill -= this.GenericCharacterMain_CanExecuteSkill;
            On.RoR2.CharacterBody.RecalculateStats -= this.OnRecalculateStats;
        }

        [Server]
        internal void PersistUpgrade(int skillLevel, string targetBaseSkillName)
        {
            if (targetBaseSkillName == null)
            {
                return;
            }
            transferrableSkillUpgrades[targetBaseSkillName] = skillLevel;
        }

        private void OnBodyStart(CharacterBody body)
        {
            this.isSurvivorEnabled = !SkillOptions.disabledSurvivorList.Contains(body.GetDisplayName());
            Log.Debug($"OnBodyStart({body})");
            // attempt to transfer and apply skill levels
            foreach (GenericSkill genericSkill in body.GetComponents<GenericSkill>())
            {
                SkillUpgradeHelper.SkillUpgrades returnVal = SkillUpgradeHelper.GetOrAddMasterSkillUpgrade(playerCharacterMasterController.master, genericSkill);
                Log.Debug($"attempting to add master skill upgrade !! {returnVal.skillNameToken}");
                returnVal.RefreshUpgrades();
                genericSkill.RecalculateValues();
            }
            
            /*var skillUpgrades = body.GetComponents<SkillUpgrade>();
            foreach (var skillUpgrade in skillUpgrades)
            {
                skillUpgrade.SetSkillPointsController(this);
            }
            TransferSkillUpgrades(body);*/
        }

        private bool bMorphedToHeretic;

        /*[Server]
        private void TransferSkillUpgrades(CharacterBody body)
        {
            unspentSkillPoints = SkillPointsAtLevel((int)body.level);

            //If this is the first time we morph into heretic, Heretic skill levels will be reset to allow respec'ing.
            bool bFirstHereticMorph = body.baseNameToken == "HERETIC_BODY_NAME" && !bMorphedToHeretic;

            var skillUpgrades = body.GetComponents<SkillUpgrade>();
            foreach (var skillUpgrade in skillUpgrades)
            {
                if (!isSurvivorEnabled) break;
                if (skillUpgrade.targetBaseSkillName != null && transferrableSkillUpgrades.ContainsKey(skillUpgrade.targetBaseSkillName))
                {
                    skillUpgrade.skillLevel = (bFirstHereticMorph ? 0 : transferrableSkillUpgrades[skillUpgrade.targetBaseSkillName]);
                    unspentSkillPoints -= skillUpgrade.skillLevel;
                    transferrableSkillUpgrades.Remove(skillUpgrade.targetBaseSkillName);
                    //skillUpgrade.OnBuySkill();
                }
            }

            if (bFirstHereticMorph)
            {
                bMorphedToHeretic = true;
            }
        }*/

        #region Hooks

        private bool GenericCharacterMain_CanExecuteSkill(On.EntityStates.GenericCharacterMain.orig_CanExecuteSkill orig, GenericCharacterMain self, GenericSkill skillSlot)
        {
            if (this.isSurvivorEnabled && this.body != null && self.outer.commonComponents.characterBody == this.body)
            {
                InputBankTest inputPlayer = this.playerCharacterMasterController?.body.inputBank;
                if (inputPlayer != null && inputPlayer.GetButtonState(SkillOptions.hotkey).down && SkillOptions.disableInput.Value && unspentSkillPoints > 0)
                {
                    return false;
                }
            }
            return orig(self, skillSlot);
        }

        private int GetDeployableSameSlotLimit(On.RoR2.CharacterMaster.orig_GetDeployableSameSlotLimit orig, CharacterMaster self, DeployableSlot slot)
        {
            int bonusSlots = 0;
            if (this.isSurvivorEnabled && self == this.playerCharacterMasterController.master)
            {
                bonusSlots = EngiSkillModifier.GetDeployableSameSlotBonus(slot);
            }
            return orig(self, slot) + bonusSlots;
        }

        private void OnRecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self == this.body)
            {
                this.OnLevelChanged();
            }
        }

        #endregion

        private void Update()
        {
#if DEBUG
            if (Input.GetKeyDown(KeyCode.Equals) && this.PlayerTeamIndex != TeamIndex.None)
            {
                //TeamManager.instance.GiveTeamExperience(body.teamComponent.teamIndex, (ulong)(500 * Time.deltaTime));
                //TeamManager.instance.SetTeamLevel(this.PlayerTeamIndex, TeamManager.instance.GetTeamLevel(this.PlayerTeamIndex) + 1);
                CmdGiveSkillPoint();
            }
            
            if (Input.GetKeyDown(KeyCode.Minus) && this.PlayerTeamIndex != TeamIndex.None)
            {
                foreach (GenericSkill genericSkill in body.GetComponents<GenericSkill>())
                {
                    SkillUpgradeHelper.SkillUpgrades returnVal = SkillUpgradeHelper.GetOrAddMasterSkillUpgrade(playerCharacterMasterController.master, genericSkill);
                    returnVal.RefreshUpgrades();
                    genericSkill.RecalculateValues();
                    Log.Debug($"refreshed skill {returnVal.skillNameToken}");
                    //skillUpgrade.targetGenericSkill = genericSkill;
                }
                Log.Debug($"$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                foreach (List<SkillUpgradeHelper.SkillUpgrades> genericSkill in SkillUpgradeHelper.masterSkills.Values)
                {
                    foreach (SkillUpgradeHelper.SkillUpgrades skillUpgrades in genericSkill)
                    {
                        Log.Debug(skillUpgrades.skillNameToken);
                    }
                }
            }
            
            if (Input.GetKeyDown(KeyCode.LeftBracket) && this.PlayerTeamIndex != TeamIndex.None)
            {
                SkillUpgradeHelper.masterSkills =
                    new Dictionary<CharacterMaster, List<SkillUpgradeHelper.SkillUpgrades>>();
                Log.Debug("cleared ");
            }
#endif
        }

        public void DeductSkillPoints(int amount)
        {
            this.unspentSkillPoints -= amount;
        }

        [Server]
        public void OnLevelChanged()
        {
            int characterLevel = (int)TeamManager.instance.GetTeamLevel(this.PlayerTeamIndex);
            //Logger.Warn("OnLevelChanged({0}) for team {1}", characterLevel, PlayerTeamIndex);
            if (this.PlayerTeamIndex == TeamIndex.None)
            {
                return;
            }

            levelsPerSkillPoint = SkillOptions.levelsPerSkillPoint.Value;
            int newSkillPoints = Math.Max(0, SkillPointsAtLevel(characterLevel) - earnedSkillPoints);

            earnedSkillPoints += newSkillPoints;
            unspentSkillPoints += newSkillPoints;
        }

        [Command]
        [Server]
        private void CmdGiveSkillPoint()
        {
            Log.Debug("Giving points");
            this.earnedSkillPoints++;
            this.unspentSkillPoints++;
        }

        private int SkillPointsAtLevel(int characterLevel)
        {
#if DEBUG
            return characterLevel - 1;
#else
            if (levelsPerSkillPoint <= 1) {
                return characterLevel - 1;
            }
            return (characterLevel / this.levelsPerSkillPoint);
#endif
        }

        [Command]
        public void CmdOnBuySkill(SkillUpgradeHelper.SkillUpgrades skillUpgrades, GenericSkill genericSkill) {
            if (!isSurvivorEnabled) return;
            if (!skillUpgrades.CanUpgradeSkill()) return;
            
            DeductSkillPoints(1);
            skillUpgrades.skillLevel += 1;
            SkillUpgradeHelper.GetOrAddMasterSkillUpgrade(playerCharacterMasterController.master, genericSkill, skillUpgrades.skillLevel);
            Log.Debug($"CmdOnBuySkill({skillUpgrades.skillNameToken}): skillLevel: {skillUpgrades.skillLevel}");


            //skillUpgrade.RefreshUpgrades();
            /*var allSkillUpgrades = this.GetComponents<SkillUpgrade>();
            foreach (var skillUpgrade in allSkillUpgrades) {
                if (skillUpgrade.targetBaseSkillName == targetSkillName) {

                }
            }*/
        }
    }
}