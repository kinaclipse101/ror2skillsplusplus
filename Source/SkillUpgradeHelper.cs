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
            var modifier = SkillModifierManager.GetSkillModifier(genericSkill.skillDef);
            foreach (var skillUpgradeIterate in skills)
            {
                if (skillUpgradeIterate.skillModifier != modifier) continue;
                
                skillUpgrade = skillUpgradeIterate;
                break;
            }
            
            if(skillUpgrade != null)
            {
                skillUpgrade.skillDef = genericSkill.skillDef;
                if (newLevel == -1) return skillUpgrade; // -1 = just getting the skill rather than upgrading ,., 
                
                skillUpgrade.skillLevel = newLevel;
                skillUpgrade.RefreshUpgrades();
                genericSkill.RecalculateValues();
                return skillUpgrade;
            }

            skillUpgrade = new SkillUpgrades(characterMaster, genericSkill.skillDef, 0, genericSkill.skillNameToken);
            skills.Add(skillUpgrade);
        }
        else
        {
            skillUpgrade = new SkillUpgrades(characterMaster, genericSkill.skillDef, 0, genericSkill.skillNameToken);
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
        public SkillDef skillDef;
        public BaseSkillModifier skillModifier;
        public int skillLevel;
        public string skillNameToken;

        public SkillUpgrades(CharacterMaster characterMaster, SkillDef skillDef, int skillLevel, string skillNameToken)
        {
            this.characterMaster = characterMaster;
            this.skillPointsController = characterMaster.GetComponent<SkillPointsController>();
            this.skillDef = skillDef;
            skillModifier = SkillModifierManager.GetSkillModifier(skillDef);
            //Log.Debug($"made new skill def from {skillModifier.skillNames[0]} skillModifier {skillDef.skillNameToken}");
            this.skillLevel = skillLevel;
            this.skillNameToken = skillNameToken;

            if (characterMaster)
            {
                characterMaster.onBodyStart += CharacterMasterOnonBodyStart;
            }
        }

        private void CharacterMasterOnonBodyStart(CharacterBody characterBody)
        {
            
        }

        public void RefreshUpgrades()
        {
            if (skillModifier != null) {
                skillModifier.OnSkillLeveledUp(skillLevel, characterMaster.GetBody(), skillDef);
            }
        }
        
        public bool CanUpgradeSkill() {
            if (!skillPointsController.isSurvivorEnabled) return false;
            if (skillPointsController && !skillPointsController.hasUnspentPoints) return false;
            return SkillModifierManager.HasSkillModifier(skillNameToken);
        }
    }
}