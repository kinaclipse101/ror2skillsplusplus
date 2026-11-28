using EntityStates;
using EntityStates.Seeker;
using RoR2;
using RoR2.Skills;
using SkillsPlusPlus.Modifiers;

namespace SkillsPlusPlus.Source.Modifiers
{
    [SkillLevelModifier("SEEKER_PRIMARY_NAME", typeof(SpiritPunch))]
    class SeekerPunchSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);
            if (skillState is SpiritPunch)
            {
                Log.Debug("SpiritPunch");
            }
        }
    }

    [SkillLevelModifier("SEEKER_SECONDARY_NAME", typeof(UnseenHand))]
    class SeekerUnseenHandSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is UnseenHand)
            {
                Log.Debug("UnseenHand");
            }
        }
    }

    [SkillLevelModifier("SEEKER_SECONDARY_ALT1_NAME", typeof(SoulSpiral))]
    class SeekerSoulSpiralSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is SoulSpiral)
            {
                Log.Debug("SoulSpiral");
            }
        }
    }

    [SkillLevelModifier("SEEKER_UTILITY_NAME", typeof(Sojourn), typeof(SojournVehicle))]
    class SeekerSojournSkillModifier : BaseSkillModifier
    {
        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is Sojourn)
            {
                Log.Debug("Sojourn");
            } 
        }
        public override void SetupSkill()
        {
            base.SetupSkill();

            On.RoR2.VehicleSeat.OnPassengerEnter += (orig, self, passenger) =>
            {
                orig(self, passenger);
                if (self.name.Contains("SojournVehicle"))
                {
                    Log.Debug("sojourn started !!!");
                    var car = self.GetComponent<SojournVehicle>();
                    if (car == null)
                    {
                        Log.Debug("erm., ,. nvm ,.,..");
                    }
                    else
                    {
                        Log.Debug("sojourn real !!");
                        car.startingSpeedBoost = 100f;
                    }
                }
            };
        }
    }

    [SkillLevelModifier("SEEKER_SPECIAL_NAME", typeof(Meditate))]
    class SeekerMeditateSkillModifier : BaseSkillModifier
    {

        public override void OnSkillLeveledUp(int level, CharacterBody characterBody, SkillDef skillDef)
        {
            base.OnSkillLeveledUp(level, characterBody, skillDef);
        }

        public override void OnSkillEnter(BaseState skillState, int level)
        {
            base.OnSkillEnter(skillState, level);

            if (skillState is Meditate)
            {
                Log.Debug("Meditate");
            }
        }
    }
}