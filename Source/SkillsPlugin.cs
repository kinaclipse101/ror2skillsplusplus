using System.Collections.Generic;
using BepInEx;
using On.EntityStates;
using RoR2;
using RoR2.UI;
using SkillsPlusPlus.Modifiers;
using SkillsPlusPlus.UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        //GameObject playerMasterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/charactermasters/CommandoMaster");
        Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Core.PlayerMaster_prefab).Completed += handle =>
        {
            handle.Result.EnsureComponent<SkillPointsController>();
        };
        
        R2API.RecalculateStatsAPI.GetStatCoefficients += LunarModifiers.RecalculateStats_GetLunarStats;

        HUD.onHudTargetChangedGlobal += HUD_onHudTargetChangedGlobal;

        On.RoR2.UI.TooltipController.SetTooltipProvider += TooltipController_SetTooltipProvider;
        On.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot += Row_FromSkillSlot;
        
        On.RoR2.SurvivorCatalog.Init += SurvivorCatalogOnInit;
        
        On.EntityStates.BaseState.OnEnter += BaseStateOnOnEnter;
        On.EntityStates.EntityState.OnExit += EntityStateOnOnExit;
    }

    private void EntityStateOnOnExit(EntityState.orig_OnExit orig, EntityStates.EntityState self)
    {
        if (self?.characterBody != null && survivors.TryGetValue(self.characterBody.baseNameToken, out SurvivorDef def) && self is EntityStates.BaseState baseState)
        {
            BaseSkillModifier skillModifier = SkillModifierManager.GetSkillModifiersForEntityStateType(self.GetType());
            if (skillModifier != null)
            {
                Log.Debug($"exit skill from survivor {def.cachedName} {skillModifier} {SkillUpgradeHelper.instance.GetStateUpgradeCount(self.characterBody.master, skillModifier)}");
                skillModifier.OnSkillExit(baseState, SkillUpgradeHelper.instance.GetStateUpgradeCount(self.characterBody.master, skillModifier));
                orig(self);
                return;
            }
        }
        
        orig(self);
    }

    private void BaseStateOnOnEnter(BaseState.orig_OnEnter orig, EntityStates.BaseState self)
    {
        if (self?.characterBody != null && survivors.TryGetValue(self.characterBody.baseNameToken, out SurvivorDef def))
        {
            BaseSkillModifier skillModifier = SkillModifierManager.GetSkillModifiersForEntityStateType(self.GetType());
            if (skillModifier != null)
            {
                Log.Debug($"entr skill from survivor {def.cachedName} {skillModifier} {SkillUpgradeHelper.instance.GetStateUpgradeCount(self.characterBody.master, skillModifier)}");
                skillModifier.OnSkillEnter(self, SkillUpgradeHelper.instance.GetStateUpgradeCount(self.characterBody.master, skillModifier));
                orig(self);
                return;
            }
        }
        
        orig(self);
    }
    
    Dictionary<string, SurvivorDef> survivors = new Dictionary<string, SurvivorDef>();
    private static void SurvivorCatalogOnInit(On.RoR2.SurvivorCatalog.orig_Init orig)
    {
        orig();
        
        foreach (SurvivorDef survivorDef in SurvivorCatalog.allSurvivorDefs)
        {
            Log.Debug($"loading survivor {survivorDef.cachedName}");
            Instance.PrepareSurvivor(survivorDef);
        }
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
        var row = orig(owner, bodyIndex, skillSlotIndex, skillSlot);

        if (row == null) return null;
            
        var buttons = row.rowData;

        for (int i = 0; i < buttons.Count; i++)
        {
            SkillsPlusPlus.Log.Debug($"Ensuring SkillsPlusPlusTooltipProvider({i})");
            var button = buttons[i];
            var skillDef = skillSlot?.skillFamily?.variants[i].skillDef;
            if (skillDef != null)
            {
                var provider = button.button.gameObject.EnsureComponent<SkillUpgradeTooltipProvider>();
                provider.skillName = ((ScriptableObject)skillDef)?.name;
                SkillsPlusPlus.Log.Debug(((ScriptableObject)skillDef)?.name);
            }
        }

        return row;
    }

    private void PrepareSurvivor(SurvivorDef survivorDef)
    {
        if (!survivorDef || !survivorDef.bodyPrefab)
            return;
        
        survivors.Add(survivorDef.bodyPrefab.GetComponent<CharacterBody>().baseNameToken, survivorDef);

        /*foreach (var genericSkill in survivorDef.bodyPrefab.GetComponents<GenericSkill>())
        {
            if (genericSkill == null)
            {
                continue;
            }
        
            var skillUpgrade = survivorDef.bodyPrefab.AddComponent<SkillUpgrade>();
            skillUpgrade.targetGenericSkill = genericSkill;
        }*/
    }

    private static void HUD_onHudTargetChangedGlobal(HUD hud)
    {
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
    }
#endif
}
