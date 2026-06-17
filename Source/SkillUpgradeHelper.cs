using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;
using UnityEngine;
using UnityEngine.Networking;

namespace SkillsPlusPlus.Source;

public class SkillUpgradeHelper
{
    public static SkillUpgradeHelper instance = new SkillUpgradeHelper();
    public Dictionary<CharacterMaster, List<SkillUpgrades>> masterSkills = [];

    public SkillUpgrades GetAddMasterSkillUpgrade(CharacterMaster characterMaster, GenericSkill genericSkill, int newLevel = -1)
    {
        SkillUpgrades skillUpgrade;
        
        if (masterSkills.TryGetValue(characterMaster, out List<SkillUpgrades> skills))
        {
            SkillUpgrades skill = skills.FirstOrDefault(skill => skill.skillName == ((ScriptableObject)genericSkill.skillDef)?.name && skill.characterMaster == characterMaster);
            if(skill != null)
            {
                skill.genericSkill = genericSkill;
                Log.Debug($"replacing generic skill ");
                if (newLevel == -1) return skill; // -1 = just getting the skill rather than upgrading ,., 
                
                skill.skillLevel = newLevel;
                skill.RefreshUpgrades();
                return skill;
            }

            skillUpgrade = new SkillUpgrades(characterMaster, genericSkill, 0, ((ScriptableObject)genericSkill.skillDef)?.name);
            skills.Add(skillUpgrade);
        }
        else
        {
            skillUpgrade = new SkillUpgrades(characterMaster, genericSkill, 0, ((ScriptableObject)genericSkill.skillDef)?.name);
            masterSkills.Add(characterMaster, [skillUpgrade]);
        }
        
        return skillUpgrade;
    }
    
    public int GetStateUpgradeCount(CharacterMaster characterMaster, BaseSkillModifier skillModifier)
    {
        if (characterMaster == null)
        {
            Log.Warning($"character master when trying to get upgrade count was null !!");
            return 0;
        }
        if (!masterSkills.TryGetValue(characterMaster, out List<SkillUpgrades> skills)) return 0;
        
        SkillUpgrades skill = skills.FirstOrDefault(upgrades => skillModifier.skillNames.Contains(upgrades.skillName));
        return skill?.skillLevel ?? 0;
    }
    
    public class SkillUpgrades
    {
        public CharacterMaster characterMaster;
        public SkillPointsController skillPointsController;
        public GenericSkill genericSkill;
        public int skillLevel;
        public string skillName;

        public SkillUpgrades(CharacterMaster characterMaster, GenericSkill genericSkill, int skillLevel, string skillName)
        {
            this.characterMaster = characterMaster;
            this.skillPointsController = characterMaster.GetComponent<SkillPointsController>();
            this.genericSkill = genericSkill;
            this.skillLevel = skillLevel;
            this.skillName = skillName;

            if (genericSkill)
            {
                genericSkill.onSkillChanged += OnSkillChanged;
            }

            if (characterMaster)
            {
                characterMaster.onBodyStart += CharacterMasterOnonBodyStart;
            }
            RefreshUpgrades();
        }

        private void CharacterMasterOnonBodyStart(CharacterBody characterBody)
        {
            
        }

        private void OnSkillChanged(GenericSkill genericSkill)
        {
            genericSkill.onSkillChanged -= OnSkillChanged;
            this.genericSkill = genericSkill;
            skillName = ((ScriptableObject)genericSkill.skillDef)?.name;
            Log.Debug($"OnSkillChanged({skillName})");
            genericSkill.onSkillChanged += OnSkillChanged;
            RefreshUpgrades();
        }

        public void RefreshUpgrades()
        {
            SkillDef activeSkillDef = genericSkill?.skillDef;
            
            if (!activeSkillDef && !genericSkill?.baseSkill)
            {
                Log.Debug($"Couldn't refresh upgrades because there is no active skill. skillName: {skillName}");
                return;
            }
            if (!activeSkillDef)
            {
                activeSkillDef = genericSkill?.baseSkill;
            }
            
            BaseSkillModifier modifier = SkillModifierManager.GetSkillModifier(activeSkillDef);
            if (modifier != null) {
                modifier.OnSkillLeveledUp(this.skillLevel, characterMaster.GetBody(), activeSkillDef);
                
                if (genericSkill) {
                    genericSkill.RecalculateValues();
                }
            }
        }
        
        public bool CanUpgradeSkill() {
            if (!skillPointsController.isSurvivorEnabled) return false;
            if (!genericSkill) return false;
            if (skillPointsController && !skillPointsController.hasUnspentPoints) return false;
            return SkillModifierManager.HasSkillModifier(skillName);
        }
    }
}