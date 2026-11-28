using System.Collections.Generic;
using BepInEx;
using On.EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using SkillsPlusPlus.Modifiers;
using SkillsPlusPlus.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityHotReloadNS;
using HealthComponent = On.RoR2.HealthComponent;
using LoadoutPanelController = RoR2.UI.LoadoutPanelController;
using Path = System.IO.Path;

//using UnityHotReloadNS;

namespace SkillsPlusPlus.Source;

[BepInDependency(R2API.R2API.PluginGUID)]
[BepInDependency("com.KingEnderBrine.ExtendedLoadout", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("com.cwmlolzlz.skills", "Skills", "0.6.4")]
[BepInDependency("pseudopulse.Rebindables")]
public sealed class SkillsPlugin : BaseUnityPlugin
{
    public static SkillsPlugin Instance;

    private void Awake()
    {
        //   _____  _     _  _  _
        //  / ____|| |   (_)| || |        _      _
        // | (___  | | __ _ | || | ___  _| |_  _| |_
        //  \___ \ | |/ /| || || |/ __||_   _||_   _|
        //  ____) ||   < | || || |\__ \  |_|    |_|
        // |_____/ |_|\_\|_||_||_||___/
        Instance = this;
        Log.Init(Logger);
        
        SkillOptions.InitConfig();
        SkillModifierManager.LoadSkillModifiers();
        
        //this is required to ensure that you can actually upgrade skills .,.,
        Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Core.PlayerMaster_prefab).Completed += handle =>
        {
            handle.Result.EnsureComponent<SkillPointsController>();
        };
        
        R2API.RecalculateStatsAPI.GetStatCoefficients += SharedRecalcStats;
        On.RoR2.HealthComponent.TakeDamageProcess += HealthComponentOnTakeDamageProcess;

        HUD.onHudTargetChangedGlobal += HUD_onHudTargetChangedGlobal;

        On.RoR2.UI.TooltipController.SetTooltipProvider += TooltipController_SetTooltipProvider;
        On.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot += Row_FromSkillSlot;
        
        On.EntityStates.BaseState.OnEnter += BaseStateOnOnEnter;
        On.EntityStates.EntityState.OnExit += EntityStateOnOnExit;
    }

    private void HealthComponentOnTakeDamageProcess(HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, DamageInfo damageinfo)
    {
        //this hurts me knowing its so many get components .,,. but its like probably fine ,.,. i thin ,.., 
        CharacterMaster master = damageinfo?.attacker?.GetComponent<CharacterBody>()?.master;
        if (!master)
        {
            orig(self, damageinfo);
            return;
        }
        
        if (!SkillUpgradeHelper.masterSkills.TryGetValue(master, out var skillUpgrades)) 
        {
            orig(self, damageinfo);
            return;
        }
        
        foreach (var skillUpgrade in skillUpgrades)
        {
            BaseSkillModifier modifier = SkillModifierManager.GetSkillModifier(skillUpgrade?.skillDef);
            modifier?.TakeDamage(skillUpgrade.skillLevel, self, damageinfo, master.GetBody());
        }
        
        orig(self, damageinfo);
        
        foreach (var skillUpgrade in skillUpgrades)
        {
            BaseSkillModifier modifier = SkillModifierManager.GetSkillModifier(skillUpgrade?.skillDef);
            modifier?.TakeDamagePost(skillUpgrade.skillLevel, self, damageinfo, master.GetBody());
        }
    }

    private void SharedRecalcStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        CharacterMaster master = sender?.master;
        if (!master) return;

        if (!SkillUpgradeHelper.masterSkills.TryGetValue(master, out var skillUpgrades)) return;
        foreach (var skillUpgrade in skillUpgrades)
        {
            BaseSkillModifier modifier = SkillModifierManager.GetSkillModifier(skillUpgrade?.skillDef);
            modifier?.RecalcStats(skillUpgrade.skillLevel, sender, args);
        }
    }

    private void EntityStateOnOnExit(EntityState.orig_OnExit orig, EntityStates.EntityState self)
    {
        if (self?.characterBody && self is EntityStates.BaseState baseState)
        {
            BaseSkillModifier skillModifier = SkillModifierManager.GetSkillModifiersForEntityStateType(self.GetType());
            if (skillModifier != null)
            {
                //Log.Debug($"exit skill from survivor {def.cachedName} {skillModifier} {SkillUpgradeHelper.GetStateUpgradeCount(self.characterBody.master, skillModifier)}");
                skillModifier.OnSkillExit(baseState, SkillUpgradeHelper.GetStateUpgradeCount(self.characterBody.master, skillModifier));
                orig(self);
                return;
            }
        }
        
        orig(self);
    }

    private void BaseStateOnOnEnter(BaseState.orig_OnEnter orig, EntityStates.BaseState self)
    {
        if (self?.characterBody)
        {
            BaseSkillModifier skillModifier = SkillModifierManager.GetSkillModifiersForEntityStateType(self.GetType());
            if (skillModifier != null)
            {
                //Log.Debug($"entr skill from survivor {def.cachedName} {skillModifier} {SkillUpgradeHelper.GetStateUpgradeCount(self.characterBody.master, skillModifier)}");
                skillModifier.OnSkillEnter(self, SkillUpgradeHelper.GetStateUpgradeCount(self.characterBody.master, skillModifier));
                orig(self);
                return;
            }
        }
        
        orig(self);
    }
    
    private static void TooltipController_SetTooltipProvider(On.RoR2.UI.TooltipController.orig_SetTooltipProvider orig, TooltipController self, TooltipProvider provider)
    {
        orig(self, provider);

        if (provider.TryGetComponent(out SkillUpgradeTooltipProvider tooltipProvider))
        {
            var tooltipController = self.EnsureComponent<SkillsPlusPlusTooltipController>();
            tooltipController.skillUpgradeToken = tooltipProvider.GetToken();
        }
    }

    private static LoadoutPanelController.Row Row_FromSkillSlot(On.RoR2.UI.LoadoutPanelController.Row.orig_FromSkillSlot orig, LoadoutPanelController owner, BodyIndex bodyIndex, int skillSlotIndex, GenericSkill skillSlot)
    {
        object row = orig(owner, bodyIndex, skillSlotIndex, skillSlot);

        if (row is not LoadoutPanelController.Row rowData) return (LoadoutPanelController.Row)row;
            
        var buttons = rowData.rowData;

        for (int i = 0; i < buttons.Count; i++)
        {
            SkillsPlusPlus.Log.Debug($"Ensuring SkillsPlusPlusTooltipProvider({i})");
            var button = buttons[i];
            var skillDef = skillSlot?.skillFamily?.variants[i].skillDef;
            if (skillDef != null)
            {
                var provider = button.button.gameObject.EnsureComponent<SkillUpgradeTooltipProvider>();
                provider.skillName = skillDef.skillNameToken;
            }
        }

        return rowData;
    }

    private static void HUD_onHudTargetChangedGlobal(HUD hud)
    {
        //its allowed 1 get components in children .,,. as a treat ,. ,.
        foreach (var skillIcon in hud.GetComponentsInChildren<SkillIcon>(true))
        {
            skillIcon.EnsureComponent<SkillUpgradeTooltipProvider>();
            skillIcon.EnsureComponent<SkillLevelIconController>();
        }
    }

#if DEBUG
    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.F4))
        {
            UnityHotReloadNS.UnityHotReload.LoadNewAssemblyVersion(
                typeof(SkillsPlugin).Assembly, // The currently loaded assembly to replace.
                System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location)!, "Skills.dll")  // The path to the newly compiled DLL.
            );
        }
        
        if (Input.GetKeyUp(KeyCode.F3))
        {
            foreach (var skillDef in SkillCatalog.allSkillDefs)
            {
                if (skillDef.skillNameToken == "") continue;
                Log.Debug($"basename {((ScriptableObject)skillDef)?.name} nametoken {skillDef.skillNameToken} real name {Language.GetStringFormatted(skillDef.skillNameToken)} skillname {skillDef.skillName}");
            }
        }
    }
#endif
}
