using System;
using UnityEngine;

namespace StealthRaider.Combat
{
    public enum Team
    {
        Hero,
        Raiders,
    }

    public class Health : MonoBehaviour
    {
        [SerializeField] float max = 100f;
        [SerializeField] Team team = Team.Raiders;

        public float Max => max;
        public float Current { get; private set; }
        public float Fraction => max > 0f ? Current / max : 0f;
        public bool IsDead => Current <= 0f;
        public Team Team => team;

        /// <summary>Amount and attacker (may be null).</summary>
        public event Action<float, GameObject> Damaged;
        public event Action Died;

        void Awake() => Current = max;

        public void Damage(float amount, GameObject attacker)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, attacker);
            if (IsDead) Died?.Invoke();
        }
    }
}
