using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;
using SkillsPlusPlus.Source;
using UnityEngine;

namespace SkillsPlusPlus
{
    public sealed class SkillModifierManager
    {
        private static readonly Dictionary<string, BaseSkillModifier> skillNameToModifierMap = new();
        private static readonly Dictionary<Type, BaseSkillModifier> typeToModifierMap = new();
        private static readonly List<BaseSkillModifier> allModifiers = [];

        /// <summary>
        /// Finds and loads all skill modifiers in the current assembly.
        /// 
        /// Calling this is essential to have your skill modifiers available to Skills++
        /// </summary>
        public static void LoadSkillModifiers()
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            foreach (Type type in assembly.GetTypes())
            {
                var attributes = type.GetCustomAttributes<SkillLevelModifierAttribute>();
                if (attributes == null || attributes != null && !attributes.Any())
                {
                    continue;
                }
                
                try
                {
                    ConstructorInfo constructorInfo = type.GetConstructor([]);
                    if (constructorInfo == null)
                    {
                        Log.Debug($"Failed to find constructor info for {type.FullName}");
                        Log.Debug("Other constructors included");
                        foreach (ConstructorInfo info in type.GetConstructors())
                        {
                            Log.Debug(info);
                        }
                        continue;
                    }
                    
                    foreach (SkillLevelModifierAttribute attribute in attributes)
                    {
                        object someSkillModifier = constructorInfo.Invoke([]);

                        if (someSkillModifier is not BaseSkillModifier skillModifier) continue;
                        
                        skillModifier.SetupSkill();
                        skillModifier.SetupConfig(SkillsPlugin.Instance.Config);

                        skillModifier.skillNames = attribute.skillNameTokens;
                        skillModifier.EntityStateTypes = attribute.baseStateTypes;
                        
                        allModifiers.Add(skillModifier);
                        
                        foreach (string skillName in attribute.skillNameTokens)
                        {
                            if (skillNameToModifierMap.TryGetValue(skillName, out BaseSkillModifier existingModifier))
                            {
                                Log.Warning("Skill modifier conflict!!!");
                                Log.Warning($"Cannot add {someSkillModifier.GetType().FullName} since {existingModifier.GetType().FullName} already exists for skill named {skillName}");
                                continue;
                            }
                            
                            skillNameToModifierMap[skillName] = skillModifier;
                        }
                        
                        foreach (Type stateType in attribute.baseStateTypes)
                        {
                            if (typeToModifierMap.TryGetValue(stateType, out BaseSkillModifier existingModifier))
                            {
                                Log.Warning("Skill modifier conflict!!!");
                                Log.Warning($"Cannot add {someSkillModifier.GetType().FullName} since {existingModifier.GetType().FullName} already exists for the entity state {stateType.FullName}");
                                continue;
                            }
                            
                            typeToModifierMap[stateType] = skillModifier;
                        }
                    }
                }
                catch (Exception error)
                {
                    Log.Error(error);
                    continue;
                }
            }
        }

        internal static BaseSkillModifier GetSkillModifier(SkillDef skillDef)
        {
            return !skillDef ? null : GetSkillModifierByName(skillDef.skillNameToken);
        }

        internal static BaseSkillModifier GetSkillModifierByName(string skillName)
        {
            return skillName == null ? null : skillNameToModifierMap.GetValueOrDefault(skillName);
        }

        internal static bool HasSkillModifier(SkillDef skillDef)
        {
            return skillDef != null && HasSkillModifier(((ScriptableObject)skillDef)?.name);
        }

        internal static bool HasSkillModifier(string baseSkillName)
        {
            return baseSkillName != null && skillNameToModifierMap.ContainsKey(baseSkillName);
        }

        internal static BaseSkillModifier GetSkillModifiersForEntityStateType(Type entityStateType)
        {
            return entityStateType == null ? null : typeToModifierMap.GetValueOrDefault(entityStateType);
        }
    }
}