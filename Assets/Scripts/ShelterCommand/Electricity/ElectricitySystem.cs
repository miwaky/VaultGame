using System;
using UnityEngine;

namespace ShelterCommand
{
    /// <summary>
    /// Central electricity manager for the shelter.
    /// Tracks a Power level (0 = off, 1 = full) and notifies all
    /// PoweredLight components when it changes.
    ///
    /// Place a single instance in the scene — other systems reference it
    /// via ElectricitySystem.Instance.
    /// </summary>
    public class ElectricitySystem : MonoBehaviour
    {
        public static ElectricitySystem Instance { get; private set; }

        [Header("Initial State")]
        [SerializeField] private bool startPowered = false;
        [SerializeField] [Range(0f, 1f)] private float defaultPower = 1f;

        /// <summary>Raised whenever power state or level changes.</summary>
        public event Action<float> OnPowerChanged;

        /// <summary>Current power level [0..1]. 0 = no power.</summary>
        public float Power { get; private set; }

        public bool IsOn => Power > 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Initialise Power silencieusement — l'event sera tiré dans Start()
            // quand tous les abonnés (PoweredLight) ont eu le temps de souscrire dans OnEnable()
            Power = startPowered ? defaultPower : 0f;
        }

        private void Start()
        {
            // Diffuse l'état initial à tous les PoweredLight déjà abonnés
            OnPowerChanged?.Invoke(Power);
        }

        /// <summary>Sets the power level and notifies all listeners.</summary>
        public void SetPower(float level)
        {
            Power = Mathf.Clamp01(level);
            OnPowerChanged?.Invoke(Power);
        }

        /// <summary>Toggles power between 0 and defaultPower.</summary>
        public void Toggle()
        {
            SetPower(IsOn ? 0f : defaultPower);
        }
    }
}
