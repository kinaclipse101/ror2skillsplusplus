using System.Collections.Generic;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;
using UnityEngine;

namespace SkillsPlusPlus.Source;

public static class SkillUpgradeHelper
{
    public static Dictionary<CharacterMaster, List<SkillUpgrades>> masterSkills = [];

    public static SkillUpgrades GetOrAddMasterSkillUpgrade(CharacterMaster characterMaster, GenericSkill genericSkill, int newLevel = -1)
    {
        SkillUpgrades skillUpgrade = null;
        
        if (masterSkills.TryGetValue(characterMaster, out List<SkillUpgrades> skills))
        {
            foreach (var skillUpgradeIterate in skills)
            {
                if (!skillUpgradeIterate.skillNameToken.Equals(genericSkill.skillNameToken)) continue;
                
                skillUpgrade = skillUpgradeIterate;
                break;
            }
            
            if(skillUpgrade != null)
            {
                skillUpgrade.genericSkill = genericSkill;
                if (newLevel == -1) return skillUpgrade; // -1 = just getting the skill rather than upgrading ,., 
                
                skillUpgrade.skillLevel = newLevel;
                skillUpgrade.RefreshUpgrades();
                return skillUpgrade;
            }

            skillUpgrade = new SkillUpgrades(characterMaster, genericSkill, 0, genericSkill.skillDef.skillNameToken);
            skills.Add(skillUpgrade);
        }
        else
        {
            skillUpgrade = new SkillUpgrades(characterMaster, genericSkill, 0, genericSkill.skillDef.skillNameToken);
            masterSkills.Add(characterMaster, [skillUpgrade]);
        }
        
        return skillUpgrade;
    }
    
    public static int GetSkillUpgradeFromSkill(GameObject bodyObject, SkillDef skillDef)
    {
        return GetSkillUpgradeFromSkill(bodyObject?.GetComponent<CharacterBody>()?.master, skillDef);
    }
    
    public static int GetSkillUpgradeFromSkill(CharacterBody body, SkillDef skillDef)
    {
        return GetSkillUpgradeFromSkill(body?.master, skillDef);
    }
    
    public static int GetSkillUpgradeFromSkill(CharacterMaster characterMaster, SkillDef skillDef)
    {
        if (!characterMaster) return 0;
        if (!masterSkills.TryGetValue(characterMaster, out List<SkillUpgrades> skills)) return 0;
        
        foreach (var skillUpgradeIterate in skills)
        {
            if (!skillUpgradeIterate.skillNameToken.Equals(skillDef.skillNameToken)) continue;
            return skillUpgradeIterate.skillLevel;
        }
        
        return 0;
    }
    
    public static int GetSkillUpgradeFromName(GameObject bodyObject, string skillToken)
    {
        return GetSkillUpgradeFromName(bodyObject?.GetComponent<CharacterBody>()?.master, skillToken);
    }
    
    public static int GetSkillUpgradeFromName(CharacterBody body, string skillToken)
    {
        return GetSkillUpgradeFromName(body?.master, skillToken);
    }
    
    public static int GetSkillUpgradeFromName(CharacterMaster characterMaster, string skillToken)
    {
        if (!characterMaster) return 0;
        if (!masterSkills.TryGetValue(characterMaster, out List<SkillUpgrades> skills)) return 0;
        
        foreach (var skillUpgradeIterate in skills)
        {
            if (!skillUpgradeIterate.skillNameToken.Equals(skillToken)) continue;
            return skillUpgradeIterate.skillLevel;
        }
        
        return 0;
    }
    
    public static int GetStateUpgradeCount(CharacterMaster characterMaster, BaseSkillModifier skillModifier)
    {
        if (!characterMaster)
        {
            Log.Warning($"character master when trying to get upgrade count was null !!");
            return 0;
        }
        
        if (!masterSkills.TryGetValue(characterMaster, out List<SkillUpgrades> skills)) return 0;

        foreach (var skillUpgrade in skills)
        {
            foreach (string skillName in skillModifier.skillNames)
            {
                if (skillName == skillUpgrade.skillNameToken)
                {
                    return skillUpgrade.skillLevel;
                }
            }
        }
        
        return 0;
    }
    
    public class SkillUpgrades
    {
        public CharacterMaster characterMaster;
        public SkillPointsController skillPointsController;
        public GenericSkill genericSkill;
        public int skillLevel;
        public string skillNameToken;

        public SkillUpgrades(CharacterMaster characterMaster, GenericSkill genericSkill, int skillLevel, string skillNameToken)
        {
            this.characterMaster = characterMaster;
            this.skillPointsController = characterMaster.GetComponent<SkillPointsController>();
            this.genericSkill = genericSkill;
            this.skillLevel = skillLevel;
            this.skillNameToken = skillNameToken;

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
            skillNameToken = genericSkill.skillDef.skillNameToken;
            genericSkill.onSkillChanged += OnSkillChanged;
            RefreshUpgrades();
        }

        public void RefreshUpgrades()
        {
            SkillDef activeSkillDef = genericSkill?.skillDef;
            
            if (!activeSkillDef && !genericSkill?.baseSkill)
            {
                Log.Debug($"Couldn't refresh upgrades because there is no active skill. skillNameToken: {skillNameToken}");
                return;
            }
            if (!activeSkillDef)
            {
                activeSkillDef = genericSkill?.baseSkill;
            }
            
            BaseSkillModifier modifier = SkillModifierManager.GetSkillModifier(activeSkillDef);
            if (modifier != null) {
                modifier.OnSkillLeveledUp(skillLevel, characterMaster.GetBody(), activeSkillDef);
                
                if (genericSkill) {
                    genericSkill.RecalculateValues();
                }
            }
        }
        
        public bool CanUpgradeSkill() {
            if (!skillPointsController.isSurvivorEnabled) return false;
            if (!genericSkill) return false;
            if (skillPointsController && !skillPointsController.hasUnspentPoints) return false;
            return SkillModifierManager.HasSkillModifier(skillNameToken);
        }
    }
}