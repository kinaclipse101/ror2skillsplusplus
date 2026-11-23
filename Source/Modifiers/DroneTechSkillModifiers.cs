using EntityStates;
using EntityStates.Drone.Command;
using EntityStates.DroneTech;
using EntityStates.DroneTech.Weapon;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace SkillsPlusPlus.Source.Modifiers
{
    [SkillLevelModifier("DRONETECH_PRIMARY_NAME", typeof(FireNanoPistol))]
    class DroneTechFireNanoPistolSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);
            if (skillState is FireNanoPistol)
            {
                Log.Debug("FireNanoPistol");
            }
        }
    }

    [SkillLevelModifier("DRONETECH_SECONDARY_NAME", typeof(Command), typeof(CommandCarry))]
    class DroneTechCommandSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is Command)
            {
                Log.Debug("Command");
            }
        }
    }

    [SkillLevelModifier("DRONETECH_SECONDARY_ALT_NAME", typeof(CommandHeadbutt))]
    class DroneTechCommandHeadbuttSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is CommandHeadbutt)
            {
                Log.Debug("CommandHeadbutt");
            }
        }
    }

    [SkillLevelModifier("DRONETECH_UTILITY_NAME", typeof(DroneLeap), typeof(DroneLeapRepeat))]
    class DroneTechDroneLeapSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is DroneLeap)
            {
                Log.Debug("DroneLeap");
            } 
            if (skillState is DroneLeapRepeat)
            {
                Log.Debug("DroneLeapRepeat");
            } 
        }
    }

    [SkillLevelModifier("DRONETECH_UTILITY_ALT_NAME", typeof(CommandShield))]
    class DroneTechCommandShieldFormationSkillModifier : BaseSkillModifier
    {

        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is CommandShield)
            {
                Log.Debug("CommandShield");
            } 
        }
    }
    
    [SkillLevelModifier("DRONETECH_SPECIAL_NAME", typeof(ThrowDroneBall))]
    class DroneTechDroneBallSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is ThrowDroneBall)
            {
                Log.Debug("ThrowDroneBall");
            } 
        }
    }
    
    [SkillLevelModifier("DRONETECH_SPECIAL_ALT_NAME", typeof(ThrowDroneBallShootable))]
    class DroneTechDroneBallShootableSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is ThrowDroneBallShootable)
            {
                Log.Debug("ThrowDroneBallShootable");
            } 
        }
    }
}