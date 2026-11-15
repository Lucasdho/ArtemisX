using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>The exposed solar field: Active makes Power but storms can break it, Retracted is safe, Broken needs Parts.</summary>
    public class SolarFieldManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private ResourceManager resources;

        public event Action<SolarFieldState> SolarStateChanged;
        public event Action<BaseComponent> ComponentBroken;
        public event Action<BaseComponent> ComponentRepaired;

        /// <summary>Returns a value in [0, 1). Replace in tests for deterministic rolls.</summary>
        public Func<float> RandomValue { get; set; } = () => UnityEngine.Random.value;

        public SolarFieldState State { get; private set; } = SolarFieldState.Active;
        public bool InputLocked { get; set; }
        public int RepairCost => config != null ? config.repairPartsCost : 5;

        /// <summary>Extra Power per day from the field (dome power is in GameConfig's Power dailyBaseChange).</summary>
        public int PowerPerDay => State == SolarFieldState.Active && config != null ? config.solarFieldPowerPerDay : 0;

        public void Initialize(GameConfig gameConfig, ResourceManager resourceManager)
        {
            config = gameConfig;
            resources = resourceManager;
        }

        private void Awake()
        {
            if (config == null || resources == null)
                Debug.LogError("SolarFieldManager needs GameConfig and ResourceManager.", this);
        }

        private void Start() => SolarStateChanged?.Invoke(State);

        /// <summary>Switches Active and Retracted during the decision phase. No cost. False if Broken or locked.</summary>
        public bool ToggleRetract()
        {
            if (InputLocked || State == SolarFieldState.Broken) return false;
            SetState(State == SolarFieldState.Active ? SolarFieldState.Retracted : SolarFieldState.Active);
            return true;
        }

        /// <summary>Storm hit during night resolution: an Active field may break. Returns true if it broke.</summary>
        public bool ResolveStorm()
        {
            if (State != SolarFieldState.Active) return false;
            float chance = config != null ? config.solarStormBreakChance : 0f;
            if (chance <= 0f || RandomValue() >= chance) return false;

            SetState(SolarFieldState.Broken);
            ComponentBroken?.Invoke(BaseComponent.SolarField);
            return true;
        }

        /// <summary>Start of a new day: a Retracted field deploys again by itself. Broken stays broken. True if it changed.</summary>
        public bool AutoRedeploy()
        {
            if (State != SolarFieldState.Retracted) return false;
            SetState(SolarFieldState.Active);
            return true;
        }

        public ActionBlockReason CanRepair()
        {
            if (InputLocked) return ActionBlockReason.InputLocked;
            if (State != SolarFieldState.Broken) return ActionBlockReason.NothingToRepair;
            if (resources == null || !resources.Has(ResourceType.Parts, RepairCost)) return ActionBlockReason.NotEnoughParts;
            return ActionBlockReason.None;
        }

        /// <summary>Pays the repair Parts and puts the field back to Active.</summary>
        public bool TryRepairSolar()
        {
            if (CanRepair() != ActionBlockReason.None) return false;
            resources.Add(ResourceType.Parts, -RepairCost);
            SetState(SolarFieldState.Active);
            ComponentRepaired?.Invoke(BaseComponent.SolarField);
            return true;
        }

        private void SetState(SolarFieldState state)
        {
            if (State == state) return;
            State = state;
            SolarStateChanged?.Invoke(state);
        }
    }
}
